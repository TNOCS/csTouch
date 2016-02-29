using System.Text.RegularExpressions;
using System;
using System.Collections.Generic;
using System.Linq;
using Caliburn.Micro;
using System.Collections.Concurrent;
using System.Globalization;
using DataServer;
using csShared.Utils;
using System.Windows;

namespace csCommon.Plugins.EffectAppraisalPlugin.Models
{
    public enum EffectLevel
    {
        None, Low, Medium, High
    }

    public class ResourceState
    {
        public ResourceState(string name) {
            Name = name;
        }

        public string Name { get; private set; }
        public int Capacity { get; private set; }

        public void AddCapacity(string capacity)
        {
            int cap;
            if (!int.TryParse(capacity, NumberStyles.Any, CultureInfo.InvariantCulture, out cap)) return;
            Capacity += cap;
        }
        public void AddCapacityAsNumber(int capacity)
        {
            Capacity += capacity;
        }

        public void AddResourceState(ResourceState state)
        {
            Capacity += state.Capacity;
        }

        public override string ToString()
        {
            return string.Format("Name: {0}, capacity: {1}", Name, Capacity);
        }
    }

    /// <summary>
    /// The EffectState keeps track of all positive and negative contributions of a certain effect. 
    /// The positive effects increase its total score, the negative effects reduce it. 
    /// Note that a positive effect can still be undesirable, e.g. the total threat score.
    /// </summary>
    public class EffectState
    {
        private int positiveScore;
        private int negativeScore;
        private bool isDisturbance;

        public EffectState(string name)
        {
            Name = name;
        }

        public EffectState(string name, EffectLevel level) : this(name)
        {
            addEffect(level);
        }

        public EffectState(string name, EffectLevel level, bool isDisturbance) : this(name, level)
        {
            this.isDisturbance = isDisturbance;
        }

        private void addEffect(EffectLevel level)
        {
            var levelScore = levelToScore(level);
            if (levelScore <= 0) return;
            Count++;
            positiveScore += levelScore;
        }

        public void AddEffectState(EffectState state)
        {
            if (state.Name != Name) return;
            Count += state.Count;
            positiveScore += state.PositiveScore;
            //negativeScore += state.NegativeScore;
        }

        public void SubtractEffectState(EffectState state)
        {
            if (state.Name != Name) return;
            negativeScore += state.PositiveScore;
        }

        public string Name { get; private set; }

        /// <summary>
        /// Number of effects that have an effect
        /// </summary>
        public int Count { get; private set; }

        /// <summary>
        /// Returns the average effect score, a value between 0 and 100.
        /// </summary>
        public double AverageScore
        {
            get
            {
                return TotalScore / Count;
            }
        }

        /// <summary>
        /// Returns the total cumulative effect score, a value between 0 and 100.
        /// </summary>
        public int TotalScore
        {
            get
            {
                return Math.Max(0, positiveScore - negativeScore);
            }
        }

        public int PositiveScore { get { return positiveScore; } }
        public int NegativeScore { get { return negativeScore; } }

        public string Overshoot {  get { return negativeScore <= positiveScore ? string.Empty : string.Format("> {0} !", negativeScore - positiveScore); } }
        public bool IsDisturbance {  get { return isDisturbance; } }

        private int levelToScore(EffectLevel level)
        {
            switch(level)
            {
                case EffectLevel.Low:    return  25;
                case EffectLevel.Medium: return  50;
                case EffectLevel.High:   return 100;
                default: return 0;
            }
        }

        public override string ToString()
        {
            return string.Format("Name: {0}, total: {1}, average: {2}, count: {3}, posScore: {4}, negScore: {5}", Name, TotalScore, AverageScore, Count, positiveScore, negativeScore);
        }
    }

    public class PoIState
    {
        const string InfluenceLabel      = "EAM.Influence";
        const string CombinedEffectLabel = "EAM.Effect.Combined.Level";
        const string RadiusLabel         = "Circle.CircleRadius";
        const string StartAngleLabel = "Circle.StartAngle";
        const string EndAngleLabel = "Circle.EndAngle";

        public PoIState(PoI poi)
        {
            PoI = poi;
            CombinedEffects = new List<EffectState>();
        }

        /// <summary>
        /// Create a PoI state object for a threat
        /// </summary>
        /// <param name="poi"></param>
        /// <param name="effects"></param>
        public PoIState(PoI poi, List<EffectState> effects) : this(poi)
        {
            IsThreat = true;
            Effects = effects;
            InfluencedBy = new List<PoIState>();
        }

        /// <summary>
        /// Create a PoI state object for a measure.
        /// </summary>
        /// <param name="poi"></param>
        /// <param name="effects"></param>
        /// <param name="resourceStates"></param>
        public PoIState(PoI poi, List<EffectState> effects, List<ResourceState> resourceStates) : this(poi)
        {
            IsThreat = false;
            Effects = effects;
            ResourceStates = resourceStates;
            Influences = new List<PoIState>();
            if (!poi.Labels.ContainsKey(RadiusLabel)) return;
            double inputValue;
            if (double.TryParse(poi.Labels[RadiusLabel], out inputValue)) InfluenceRadiusInMeter = inputValue;
            StartAngleInDegree = (poi.Labels.ContainsKey(StartAngleLabel) && double.TryParse(poi.Labels[StartAngleLabel], out inputValue)) ? inputValue : 0;
            EndAngleInDegree   = (poi.Labels.ContainsKey(EndAngleLabel  ) && double.TryParse(poi.Labels[EndAngleLabel  ], out inputValue)) ? inputValue : 360;
            if (EndAngleInDegree < StartAngleInDegree) EndAngleInDegree += 360;
        }

        public bool IsThreat { get; private set; }
        public PoI PoI { get; private set; }
        public List<PoIState> Influences { get; private set; }
        public List<PoIState> InfluencedBy { get; private set; }
        public double InfluenceRadiusInMeter { get; private set; }
        public double StartAngleInDegree { get; private set; }
        public double EndAngleInDegree { get; private set; }
        public Position Position { get { return PoI.Position; } }
        public List<ResourceState> ResourceStates { get; private set; }
        public List<EffectState> Effects { get; private set; }
        /// <summary>
        /// Effect that remains when all effects and influence are combined.
        /// </summary>
        public List<EffectState> CombinedEffects { get; private set; }

        /// <summary>
        /// Write the influence (and combined effect) to the label
        /// </summary>
        public void UpdatePoI()
        {
            if (IsThreat)
            {
                PoI.Labels[InfluenceLabel] = string.Join(Environment.NewLine, InfluencedBy.Select(ps => ps.PoI.Name));
                // Update combined effect
                if (CombinedEffects.Count > 0)
                    PoI.Labels[CombinedEffectLabel] = string.Format("({0}) => {1}", InfluencedBy.Count, CombinedEffects[0].AverageScore);
            }
            else
            {
                PoI.Labels[InfluenceLabel] = string.Join(Environment.NewLine, Influences.Select(ps => ps.PoI.Name));
            }
            PoI.TriggerLabelChanged(InfluenceLabel);
        }

        public override string ToString()
        {
            return string.Format("Name: {0}, isThreat: {1}, influenceRadius: {2}", PoI.Name, IsThreat, InfluenceRadiusInMeter);
        }
    }

    public class PhaseState : PropertyChangedBase
    {
        const string InfluenceLabel          = "EAM.Influence";
        const string InitialThreatScoreLabel = "EAM.InitialThreatScore";
        const string FinalThreatScoreLabel   = "EAM.FinalThreatScore";
        const string DisturbanceLevelLabel   = ".DisturbanceLevel";

        private readonly string title;
        private readonly List<PoIState> threats  = new List<PoIState>();
        private readonly List<PoIState> measures = new List<PoIState>();
        private readonly BindableCollection<PhaseState> phaseStates                     = new BindableCollection<PhaseState>();
        private readonly ConcurrentDictionary<string, EffectState> threatStates         = new ConcurrentDictionary<string, EffectState>();
        private readonly ConcurrentDictionary<string, EffectState> measureStates        = new ConcurrentDictionary<string, EffectState>();
        private readonly ConcurrentDictionary<string, ResourceState> resourceStates     = new ConcurrentDictionary<string, ResourceState>();
        //private string activePhaseTitle;

        public PhaseState(string title) { this.title = title; }

        public string Title { get { return title; } }
        public List<PoIState> Threats { get { return threats; } }
        public List<PoIState> Measures { get { return measures; } }
        public BindableCollection<PhaseState> PhaseStates { get { return phaseStates; } }
        public ICollection<EffectState> ThreatStates { get { return threatStates.Values; } }
        public ICollection<EffectState> MeasureStates { get { return measureStates.Values; } }
        public ICollection<ResourceState> ResourceStates { get { return resourceStates.Values; } }

        public void Reset()
        {
            //phaseStates   .Clear();
            threatStates  .Clear();
            measureStates .Clear();
            resourceStates.Clear();
            foreach (var phase in PhaseStates)
            {
                phase.Measures.Clear();
                phase.Threats.Clear();
                phase.resourceStates.Clear();
            }
            Measures.Clear();
            Threats.Clear();
        }

        /// <summary>
        /// Make the current state active, i.e. the PoIs and dashboard represent the current phase.
        /// </summary>
        public void Activate()
        {
            //Reset();
            threatStates.Clear();
            measureStates.Clear();
            //resourceStates.Clear();
            //if (string.IsNullOrEmpty(title)) return;
            //activePhaseTitle = title;
            //if (!phaseStates.ContainsKey(title)) return;
            //var activePhase = phaseStates[title];
            UpdateState(this);
            foreach (var ts in ThreatStates  ) threatStates  .TryAdd(ts.Name, ts);
            foreach (var ts in MeasureStates ) measureStates .TryAdd(ts.Name, ts);
            //foreach (var ts in ResourceStates) resourceStates.TryAdd(ts.Name, ts);
            foreach (var threat  in Threats ) threat .UpdatePoI();
            foreach (var measure in Measures) measure.UpdatePoI();
        }

        public void AddThreat(PoI bc) {
            add(bc, true);
        }

        public void AddMeasure(PoI bc) { add(bc, false); }

        private void add(PoI poi, bool isThreat)
        {
            var effectStates   = new List<EffectState>();
            var resourceStates = new List<ResourceState>();
            var activePhases   = new List<string>();

            // Clear all influences
            poi.Labels[InfluenceLabel] = string.Empty; 

            // Process the labels
            foreach (var label in poi.Labels)
            {
                // Test to see if there is a match in the InputText
                var match = regex.Match(label.Key);
                if (!match.Success) continue;

                var eamType = match.Groups[1].Value.ToLower(); // Index 0 represents the capture
                var title   = match.Groups[2].Value.ToLower();
                switch (eamType)
                {
                    case "effect":
                        if (string.IsNullOrEmpty(label.Value)) continue;
                        EffectLevel level;
                        if (!Enum.TryParse<EffectLevel>(label.Value, out level) || level == EffectLevel.None) continue;
                        var isDisturbance = label.Key.EndsWith(DisturbanceLevelLabel);
                        var effect = new EffectState(title, level, isDisturbance);
                        effectStates.Add(effect);
                        break;
                    case "capacity":
                        if (string.IsNullOrEmpty(label.Value) || string.Equals(label.Value, "0", StringComparison.InvariantCultureIgnoreCase)) continue;
                        var resourceState = new ResourceState(title);
                        resourceState.AddCapacity(label.Value);
                        resourceStates.Add(resourceState);
                        break;
                    case "phase":
                        if (TryGetPhaseState(title) == null) phaseStates.Add(new PhaseState(title));
                        if (!string.Equals(label.Value, "true", StringComparison.InvariantCultureIgnoreCase)) continue;
                        activePhases.Add(title);
                        break;
                }
            }

            // Update each phase that is affected
            foreach (var title in activePhases)
            {
                var phase = TryGetPhaseState(title);
                if (phase == null) return;
                if (isThreat)
                    phase.Threats .Add(new PoIState(poi, effectStates));
                else
                    phase.Measures.Add(new PoIState(poi, effectStates, resourceStates));
                foreach (var rs in resourceStates)
                {
                    ResourceState resourceState;
                    if (phase.resourceStates.TryGetValue(rs.Name, out resourceState)) resourceState.AddCapacityAsNumber(rs.Capacity);
                    else phase.resourceStates.TryAdd(rs.Name, rs);
                }
            }
        }

        private PhaseState TryGetPhaseState(string title)
        {
            return PhaseStates.FirstOrDefault(ps => string.Equals(title, ps.Title, StringComparison.InvariantCultureIgnoreCase));
        }

        /// <summary>
        /// Check whether the point is in the arc segment.
        /// </summary>
        /// <param name="center">Center point of the arc segment.</param>
        /// <param name="point">Point to check.</param>
        /// <param name="startAngle">Angle in degree, where North is zero.</param>
        /// <param name="endAngle">Angle in degree, where North is zero.</param>
        /// <param name="radiusInMeter">Radius in meters.</param>
        /// <returns>True if the point is within the arc segment, otherwise false.</returns>
        private bool isInArcSegment(Position center, Position point, double startAngle, double endAngle, double radiusInMeter)
        {
            var distanceInMeter = CoordinateUtils.Distance(center.Latitude, center.Longitude, point.Latitude, point.Longitude) * 1000;
            if (distanceInMeter > radiusInMeter) return false;
            // Compute the angle between the vertical Y-axis and the line between center and point
            var relativePoint = new Point(point.Longitude - center.Longitude, point.Latitude - center.Latitude);
            // Compute the angle in clock wise orientation: for that reason, I've swapped the X and Y!
            var angle = CoordinateUtils.Rad2Deg(Math.Atan2(relativePoint.X, relativePoint.Y));
            // Adjust the angle for being in the third or fourth quadrant (bottom half of the clock)
            if (relativePoint.X < 0) angle += 360;
            if (startAngle < 0) startAngle += 360;
            if (endAngle   < 0) endAngle   += 360;
            return (endAngle <= startAngle) 
                ? (startAngle <= angle && angle <= 360) || (0 <= angle && angle <= endAngle)
                : (startAngle <= angle && angle <= endAngle);
        }

        private void UpdateState(PhaseState phase)
        {
            // Clear computations
            foreach (var threat in phase.Threats)
            {
                threat.InfluencedBy.Clear();
                threat.CombinedEffects.Clear();
            }
            foreach (var measure in phase.Measures)
            {
                measure.Influences.Clear();
                measure.CombinedEffects.Clear();
            }

            // Determine which measure influences which threat (and vice versa)
            foreach (var measure in phase.Measures)
            {
                if (measure.InfluenceRadiusInMeter <= 0) continue;
                foreach (var threat in phase.Threats)
                {
                    var overlap = threat.Effects.Select(effect => effect.Name).Intersect(measure.Effects.Select(effect => effect.Name)).Any();
                    if (!overlap) continue;
                    //var distanceInMeter = CoordinateUtils.Distance(measure.Position.Latitude, measure.Position.Longitude, threat.Position.Latitude, threat.Position.Longitude) * 1000;
                    //if (distanceInMeter > measure.InfluenceRadiusInMeter) continue;
                    //var relPoint = new Point(threat.Position.Longitude - measure.Position.Longitude, threat.Position.Latitude - measure.Position.Latitude);
                    if (!isInArcSegment(measure.Position, threat.Position, measure.StartAngleInDegree, measure.EndAngleInDegree, measure.InfluenceRadiusInMeter)) continue;
                    threat.InfluencedBy.Add(measure);
                    measure.Influences.Add(threat);
                }
            }
            // Compute the effects of this influence: threat is reduced, measure effectiveness is increased, labels are updated
            foreach (var threat in phase.Threats)
            {
                foreach (var threatEffect in threat.Effects)
                {
                    var combinedEffect = new EffectState(threatEffect.Name);
                    combinedEffect.AddEffectState(threatEffect);
                    foreach (var measure in threat.InfluencedBy)
                    {
                        foreach (var measureEffect in measure.Effects)
                        {
                            combinedEffect.SubtractEffectState(measureEffect);
                        }
                    }
                    threat.CombinedEffects.Add(combinedEffect);
                    updateThreatVisualisation(threat);
                }

                // For this phase, compute the combined threat
                foreach (var ce in threat.CombinedEffects)
                {
                    if (!phase.threatStates.ContainsKey(ce.Name)) phase.threatStates[ce.Name] = ce;
                    else phase.threatStates[ce.Name].AddEffectState(ce);
                }
            }
            foreach (var measure in phase.Measures)
            {
                // For this phase, compute the combined effect
                foreach (var ce in measure.Effects)
                {
                    if (!phase.measureStates.ContainsKey(ce.Name)) phase.measureStates[ce.Name] = ce;
                    else phase.measureStates[ce.Name].AddEffectState(ce);
                }
            }
        }

        /// <summary>
        /// Set the color of the threat based on the current threat level in a label
        /// </summary>
        /// <param name="threat"></param>
        private void updateThreatVisualisation(PoIState threat)
        {
            var positiveScore = threat.CombinedEffects.Average((ce) => { return ce.PositiveScore; });
            var averageScore  = threat.CombinedEffects.Average((ce) => { return ce.AverageScore; });
            threat.PoI.Labels[InitialThreatScoreLabel] = positiveScore.ToString(CultureInfo.InvariantCulture);
            threat.PoI.Labels[FinalThreatScoreLabel]   = averageScore .ToString(CultureInfo.InvariantCulture);
        }

        public override string ToString()
        {
            return string.Format("Name: {0}", title);
        }

        /// <summary>
        ///  Regular expression built for C# on: Thu, Jun 11, 2015, 11:55:02 AM
        ///  Using Expresso Version: 3.0.4750, http://www.ultrapico.com
        ///  
        ///  A description of the regular expression:
        ///  
        ///  EAM.
        ///      EAM
        ///      Any character
        ///  [Type]: A named capture group. [[a-zA-Z]*]
        ///      Any character in this class: [a-zA-Z], any number of repetitions
        ///  Any character in this class: [.]
        ///  [Title]: A named capture group. [[a-zA-Z]*]
        ///      Any character in this class: [a-zA-Z], any number of repetitions
        ///  Any character in this class: [.]
        ///  [Property]: A named capture group. [[a-zA-Z]*]
        ///      Any character in this class: [a-zA-Z], any number of repetitions
        ///  
        ///
        /// </summary>
        public static Regex regex = new Regex(
              "EAM.(?<Type>[a-zA-Z]*)[.](?<Title>[a-zA-Z]*)[.](?<Property>[" +
              "a-zA-Z]*)",
            RegexOptions.Singleline
            | RegexOptions.CultureInvariant
            | RegexOptions.Compiled
            );
        //// Capture all Matches in the InputText
        // MatchCollection ms = regex.Matches(InputText);

        //// Test to see if there is a match in the InputText
        // bool IsMatch = regex.IsMatch(InputText);

        //// Get the names of all the named and numbered capture groups
        // string[] GroupNames = regex.GetGroupNames();

        //// Get the numbers of all the named and numbered capture groups
        // int[] GroupNumbers = regex.GetGroupNumbers();


    }
}
