using System;
using System.Collections.Generic;
using System.Linq;

namespace TspAco
{
    class Program
    {
        static void Main(string[] args)
        {
            // кол-во городов
            const int CITIES_COUNT = 20;
            // альфа - коэффициент запаха
            const double A = 1.0;
            // бета - коэффициент расстояния
            const double B = 2.0;
            // коэффициент обновления, глобальное
            const double pheromoneGlobal = 0.1;
            // коэффициент обновления, локальное
            const double pheromoneLocal = 0.1;
            // количество выпускаемых феромонов
            const double Q = 1.0;
            // баланс между лучшим городом и как в AS
            const double q = 0.9;
            // начальный феромон
            // в нашей реализации
            const double defaultPheromone = Q / (CITIES_COUNT * 2000.0);
            // кол - во итераций(поколений)
            const int epochQty = 2000;
            // кол - во муравьев в поколении
            const int antsQty = 50;

            var rand = new Random();

            // массив координат городов (x,y)
            double[,] cities = new double[CITIES_COUNT, 2];

            for (int i = 0; i < CITIES_COUNT; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    cities[i, j] = rand.NextDouble() * 100.0;
                }
            }

            // матрица расстояний между городами
            double[,] distance = new double[CITIES_COUNT, CITIES_COUNT];
            double[,] returnDistance = new double[CITIES_COUNT, CITIES_COUNT];

            // заполняем матрицу расстояний
            for (int i = 0; i < CITIES_COUNT - 1; i++)
            {
                for (int j = i + 1; j < CITIES_COUNT; j++)
                {
                    double dist = Math.Sqrt(Math.Pow(cities[i, 0] - cities[j, 0], 2.0) + Math.Pow(cities[i, 1] - cities[j, 1], 2.0));
                    distance[i, j] = distance[j, i] = dist;
                    returnDistance[i, j] = returnDistance[j, i] = 1 / dist;
                }
            }

            int bestRouteEpoch = 0;
            double bestRouteLenght = double.MaxValue;
            var bestRoute = new Route(distance);
            int bestRouteChanged = 0;

            // матрица феромонов
            double[,] pheromone = new double[CITIES_COUNT, CITIES_COUNT];

            // заполняем матрицу феромонов начальным значением
            for (int i = 0; i < CITIES_COUNT - 1; i++)
            {
                for (int j = i + 1; j < CITIES_COUNT; j++)
                {
                    pheromone[i, j] = pheromone[j, i] = defaultPheromone;
                }
            }

            // массив списков для маршрутов муравьев в одном поколении
            var antRoutes = new Route[antsQty];

            // let's go
            // запускаем алгоритм на количество эпох
            for (int epoch = 0; epoch < epochQty; epoch++)
            {
                // можно инициализировать маршрут первого муравья
                // возможные варианты:
                // - случайный маршрут
                // - разные маршруты
                // - из одного города

                // в каждой эпохе, каждый муравей ищет маршрут
                // если маршрут для 1-го муравья задан выше, то можно начать со 2-го
                for (int ant = 0; ant < antsQty; ant++)
                {
                    // инициализируем начальное расположение муравья
                    // возможны варианты:
                    // - каждый муравей располагается случайно 
                    //    antRoutes[ant] = new List<int>(CITIES_COUNT)
                    //{
                    //    rand.Next(CITIES_COUNT)
                    //};

                    // - с каждого города выходит один муравей (без совпадений)

                    // - с конкретного города originCity выходят все муравьи
                    const int ORIGIN_CITY = 0;
                    antRoutes[ant] = new Route(distance);
                    antRoutes[ant].AddNode(ORIGIN_CITY);

                    // путь каждого муравья
                    for (int step = 1; step < CITIES_COUNT; step++)
                    {
                        int currentCity = antRoutes[ant].VisitedNodes.Last();

                        // формируем массив вероятности переходов в непосещенные города
                        // Pij = (Tij^a * NUij^b) / sum(Tih * NUih);
                        // числитель
                        int[] adjacentCities = Enumerable.Range(0, CITIES_COUNT).Except(antRoutes[ant].VisitedNodes).ToArray();
                        IEnumerable<double> numerator = adjacentCities.Select(adjacentCity =>
                            Math.Pow(pheromone[currentCity, adjacentCity], A) * Math.Pow(returnDistance[currentCity, adjacentCity], B));
                        // складываем массив вероятности для получения знаменателя в формуле
                        double denominator = numerator.Sum();
                        // массив вероятности переходов из currentCity в непосещенные вершины
                        double[] probability = numerator.Select(n => n / denominator).ToArray();
                        // случайно выбираем следующий город для перехода из массива вероятностей
                        antRoutes[ant].AddNode(adjacentCities[GetNextNodeIndex(probability, rand.NextDouble())]);
                    }

                    // подсчет длины маршрута текущего муравья
                    if (antRoutes[ant].Distance < bestRouteLenght)
                    {
                        bestRouteEpoch = epoch;
                        bestRouteLenght = antRoutes[ant].Distance;
                        bestRoute = antRoutes[ant];
                        bestRouteChanged++;
                    }

                    // локальное обновление феромона, после  каждого муравья
                    for (int i = 1; i < antRoutes[ant].Count; i++)
                    {
                        int xL = antRoutes[ant][i - 1];
                        int yL = antRoutes[ant][i];
                        // ??? помоему странная формула, которая сразу учитывает испарение.
                        pheromone[xL, yL] = (1 - pheromoneLocal) * pheromone[xL, yL] + pheromoneLocal * defaultPheromone;
                        pheromone[yL, xL] = (1 - pheromoneLocal) * pheromone[yL, xL] + pheromoneLocal * defaultPheromone;
                    }
                }

                // Испаряем феромоны по всей матрице
                for (int i = 0; i < CITIES_COUNT; i++)
                {
                    for (int j = 0; j < CITIES_COUNT; j++)
                    {
                        pheromone[i, j] = (1 - pheromoneGlobal) * pheromone[i, j];
                    }
                }

                // Добавляем феромон лучшему маршруту
                for (int i = 1; i < bestRoute.Count; i++)
                {
                    int xG = bestRoute[i - 1];
                    int yG = bestRoute[i];

                    pheromone[xG, yG] = pheromone[xG, yG] + pheromoneGlobal * (Q / bestRouteLenght);
                    pheromone[yG, xG] = pheromone[yG, xG] + pheromoneGlobal * (Q / bestRouteLenght);
                }
            }

            // выводим матрицу расстояний
            //for (int i = 0; i < CITIES_COUNT; i++)
            //{
            //    for (int j = 0; j < CITIES_COUNT; j++)
            //    {
            //        Console.Write($"{distance[i, j]:g6}\t");
            //    }
            //    Console.WriteLine();
            //}

            Console.WriteLine($"Best route: lengh: {bestRouteLenght} epoch: {bestRouteEpoch} bestRouteChanged: {bestRouteChanged}");
            // выводи лучший маршрут
            foreach (int step in bestRoute.VisitedNodes)
            {
                Console.Write($"{step} ");
            }
            Console.WriteLine();

        }



        /// <summary>
        /// Получение индекса диапазона, в который будет производиться переход
        /// </summary>
        /// <param name="transProb">веротяности перехода</param>
        /// <param name="value">случайное значение</param>
        /// <returns>Индекс </returns>
        private static int GetNextNodeIndex(double[] transProb, double value)
        {
            if (!transProb.Any())
                throw new ArgumentException(@"В массиве вероятности переходов отсутствуют элементы", nameof(transProb));

            if (value < 0.0 || value > 1.0)
                throw new ArgumentException(@"Случайное значение должно находится в границах от 0 до 1", nameof(value));

            if (transProb.Length > 1)
            {
                double left = 0.0;
                for (int i = 0; i < transProb.Length; i++)
                {
                    double right = left + transProb[i];
                    if (left < value && value < right)
                    {
                        return i;
                    }

                    left = right;
                }
            }

            return 0;
        }
    }
}
