using NinjaTrader.Custom.AddOns.SecretSauce.Models;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using System.Xml.Serialization;

namespace NinjaTrader.NinjaScript.Indicators
{
    public class CumulativeDeltaTrendScore : Indicator
    {
        #region Properties

        public const string GROUP_NAME_GENERAL = "1. General";
        public const string GROUP_NAME_DEFAULT = "2. Cumulative Delta Trend Score";

        [NinjaScriptProperty, ReadOnly(true)]
        [Display(Name = "Version", Description = "Cumulative Delta Trend Score", Order = 0, GroupName = GROUP_NAME_GENERAL)]
        public string Version => "1.0.0";

        [NinjaScriptProperty]
        [Display(Name = "Trend Classifier Period", Description = "Period for trend classifier", GroupName = GROUP_NAME_DEFAULT, Order = 0)]
        public int TrendClassifierPeriod { get; set; }

        [NinjaScriptProperty, XmlIgnore]
        [Display(Name = "Positive Trend Score Color", Description = "The positive trend score color.", GroupName = GROUP_NAME_DEFAULT, Order = 1)]
        public Brush PositiveTrendScoreColor { get; set; }

        [NinjaScriptProperty, XmlIgnore]
        [Display(Name = "Negative Trend Score Color", Description = "The negative trend score color.", GroupName = GROUP_NAME_DEFAULT, Order = 2)]
        public Brush NegativeTrendScoreColor { get; set; }

        #endregion

        #region Serialization

        [Browsable(false)]
        public string PositiveTrendScoreColorSerialize
        {
            get => Serialize.BrushToString(PositiveTrendScoreColor);
            set => PositiveTrendScoreColor = Serialize.StringToBrush(value);
        }

        [Browsable(false)]
        public string NegativeTrendScoreColorSerialize
        {
            get => Serialize.BrushToString(NegativeTrendScoreColor);
            set => NegativeTrendScoreColor = Serialize.StringToBrush(value);
        }

        #endregion

        private int _primaryIndex;
        private int _tickIndex;
        private TrendClassifier _trendClassifier;
        private OrderFlowCumulativeDelta _cumulativeDelta;

        Color _negativeBrush;
        Color _positiveBrush;

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Name = "_CumulatveDeltaTrendScoreIndicator";
                Description = "CumulativeDelta";
                Calculate = Calculate.OnEachTick;
                IsOverlay = false;
                DisplayInDataBox = true;
                DrawOnPricePanel = false;
                DrawHorizontalGridLines = false;
                DrawVerticalGridLines = false;

                TrendClassifierPeriod = 8;
                PositiveTrendScoreColor = Brushes.Green;
                NegativeTrendScoreColor = Brushes.Red;

                AddPlot(
                    new Stroke(Brushes.CadetBlue, DashStyleHelper.Solid, 2),
                        PlotStyle.Bar,
                        "DeltaBody"
                );

                AddLine(
                    new Stroke(Brushes.CadetBlue, DashStyleHelper.Solid, 1),
                    0,
                    "ZeroLine"
                );
            }

            else if (State == State.Configure)
            {
                AddDataSeries(BarsPeriodType.Tick, 1);

                _trendClassifier = new TrendClassifier(TrendClassifierPeriod);

                SolidColorBrush negativeBrush = (SolidColorBrush)NegativeTrendScoreColor;
                SolidColorBrush positiveBrush = (SolidColorBrush)PositiveTrendScoreColor;
                _negativeBrush = negativeBrush.Color;
                _positiveBrush = positiveBrush.Color;

                Plots[0].AutoWidth = true;
            }
            else if (State == State.DataLoaded)
            {
                _primaryIndex = 0;
                _tickIndex = 1;
                _cumulativeDelta = OrderFlowCumulativeDelta(CumulativeDeltaType.BidAsk, CumulativeDeltaPeriod.Session, 0);
            }
        }

        protected override void OnBarUpdate()
        {
            if (BarsInProgress == _tickIndex)
            {
                _cumulativeDelta.Update(_cumulativeDelta.BarsArray[1].Count - 1, 1);
                return;
            }

            if (BarsInProgress == _primaryIndex)
            {
                Values[0][0] = _cumulativeDelta.DeltaClose[0] - 1;

                if (IsFirstTickOfBar && CurrentBars[_primaryIndex] >= TrendClassifierPeriod)
                {
                    int barAgo = 1;

                    Brush brush = GetTrendColor(_trendClassifier.CalculateTrendScore(
                        _cumulativeDelta.DeltaClose, CurrentBar - barAgo
                    ));
                    PlotBrushes[0][barAgo] = brush;
                }
            }
        }

        private static Color InterpolateColor(Color color1, Color color2, double t)
        {
            byte r = (byte)(color1.R * (1 - t) + color2.R * t);
            byte g = (byte)(color1.G * (1 - t) + color2.G * t);
            byte b = (byte)(color1.B * (1 - t) + color2.B * t);
            return Color.FromRgb(r, g, b);
        }

        // Map trend score (-1 to 1) to a gradient color between colors
        private Brush GetTrendColor(double trendScore)
        {
            // Normalize trendScore from [-1, 1] to [0, 1]
            double t = (trendScore + 1) / 2.0; // -1 -> 0, 1 -> 1
            Color color = InterpolateColor(_negativeBrush, _positiveBrush, t);
            return new SolidColorBrush(color);
        }
    }
}