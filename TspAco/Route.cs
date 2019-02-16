using System.Collections.Generic;
using System.Linq;

namespace TspAco
{
    /// <summary>
    /// Последовательность посещенных вершин
    /// </summary>
    public class Route
    {
        #region Fields

        private readonly List<int> _visitedNodes;
        private double[,] _map;

        #endregion

        #region Properties

        public IEnumerable<int> VisitedNodes => _visitedNodes;

        public int Count => _visitedNodes.Count;

        public double Distance { get; private set; }

        public int this[int i]
        {
            get => _visitedNodes[i];
            set => _visitedNodes[i] = value;
        }

        #endregion

        #region Contructors

        public Route()
        {
            _visitedNodes = new List<int>();
            Distance = 0.0;
        }

        public Route(double[,] map)
            : this()
        {
            SetMap(map);
        }

        #endregion

        #region Methods

        public void AddNode(int node)
        {
            if (_map != null && _visitedNodes.Any())
            {
                Distance += _map[_visitedNodes.Last(), node];
            }

            _visitedNodes.Add(node);
        }

        public void SetMap(double[,] map)
        {
            _map = map;
            
            // пересчет параметров маршрута для новой карты
            if (_visitedNodes.Any())
            {
                double d = 0.0;
                for (int i = 1; i < _visitedNodes.Count; i++)
                {
                    d += _map[_visitedNodes[i - 1], _visitedNodes[i]];
                }
                Distance = d;
            }
        }

        #endregion
    }
}
