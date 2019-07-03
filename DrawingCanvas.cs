using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;

namespace WpfDisplayBuilder
{
    public class DrawingCanvas : Canvas
    {
        public DrawingCanvas()
        {
            SnapsToDevicePixels = true;
        }

        private readonly List<DrawingVisual> _hits = new List<DrawingVisual>();
 
        public IEnumerable<DrawingVisual> GetVisuals(Geometry region)
        {
            // Очистить результаты предыдущего поиска
            _hits.Clear();
            // Подготовить параметры для операции проверки попадания (геометрию и обратный вызов)
            var parameters = new GeometryHitTestParameters(region);
            var callback = new HitTestResultCallback(HitTestCallback);
            // Поиск попаданий
            VisualTreeHelper.HitTest(this, null, callback, parameters);
            return _hits;
        }

        private HitTestResultBehavior HitTestCallback(HitTestResult result)
        {
            var geometryResult = (GeometryHitTestResult) result;
            var visual = result.VisualHit as DrawingVisual;
            // Попадание фиксируется, только если в точке найден объект DrawingVisual, и он целиком находится в геометрии
            if (visual != null &&
                geometryResult.IntersectionDetail == IntersectionDetail.FullyInside)
            {
                _hits.Add(visual);
            }
            return HitTestResultBehavior.Continue;
        }

        public DrawingVisual GetVisual(System.Windows.Point point)
        {
            var hitResult = VisualTreeHelper.HitTest(this, point);
            return  hitResult != null ? hitResult.VisualHit as DrawingVisual : null;
        }

        public int GetVisualIndex(Visual visual)
        {
            return _visuals.FindIndex(item => item.Equals(visual));
        }

        public void AddVisual(Visual visual)
        {
            _visuals.Add(visual);
            AddVisualChild(visual);
            AddLogicalChild(visual);
        }

        public void DeleteVisual(Visual visual)
        {
            _visuals.Remove(visual);
            RemoveVisualChild(visual);
            RemoveLogicalChild(visual);
        }

        public void Clear()
        {
            var list = new List<Visual>(_visuals);
            foreach (var visual in list)
                DeleteVisual(visual);
            Children.Clear();
        }

        private readonly List<Visual> _visuals = new List<Visual>();

        protected override int VisualChildrenCount
        {
            get { return _visuals.Count + base.VisualChildrenCount; }
        }

        protected override Visual GetVisualChild(int index)
        {
            return index < _visuals.Count ? _visuals[index] : base.GetVisualChild(index - _visuals.Count);
        }
    }
}
