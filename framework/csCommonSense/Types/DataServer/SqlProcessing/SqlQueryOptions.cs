using System;
using System.Collections.Generic;
using System.Globalization;
using csGeoLayers;
using Jace;

namespace DataServer.SqlProcessing
{
    public enum SqlInputOutputRelationship
    {
        Linear,
        Quadratic
    }

    /// <summary>
    /// In case we wish to have an iterative query, employ this class to set its options.
    /// Basically, we have four inputs and one output:
    /// - An input parameter that is varied until the desired result is reached.
    /// - An input parameter that contains the desired result.
    /// - An input parameter that determines the accuracy of the desired result.
    /// - An input parameter that determines the maximum number of tries.
    /// - An output parameter that contains the actual result.
    /// </summary>
    public class SqlQueryOptions
    {
        /// <summary>
        /// The index of the input parameter that is used to control the output 
        /// in order to reach the desired result.
        /// </summary>
        public int ControlInputParameterIndex { get; set; }

        /// <summary>
        /// The formula used to compute the desired result. For example, {1}*{2} means that we need to
        /// multiply input index 1 with input index 2.
        /// In case it's a direct relationship to an input paramenter, you can use its index directly, 
        /// e.g. {2} to use the third input parameter (remember, we start counting at 0).
        /// </summary>
        public string DesiredResultFormula { get; set; }

        /// <summary>
        /// The index of the input parameter that contains the accuracy of the desired result.
        /// The value is interpreted as a percentage of the desired result, i.e. if the desired
        /// result = 100, and the desired accuracy = 5, we stop when the actual output is between
        /// 95 and 105.
        /// </summary>
        public int DesiredAccuracyInputParameterIndex { get; set; }
        
        /// <summary>
        ///  The index of the input parameter that contains the maximum number of tries before we stop.
        /// </summary>
        public int MaxTriesInputParameterIndex { get; set; }

        /// <summary>
        /// The relationship between control input and actual outcome.
        /// It is used to improve the iteration speed.
        /// </summary>
        public SqlInputOutputRelationship Relationship { get; set; }

        /// <summary>
        /// The index of the output parameter that contains the actual result.
        /// </summary>
        public int ActualResultOutputParameterIndex { get; set; }

        /// <summary>
        /// The name of the label that should contain the computed desired result.
        /// </summary>
        public string DesiredResultLabel { get; set; }

        #region Non-serialized part that computes the desired result 

        private object currentMathFormula;

        /// <summary>
        /// Converts the formula to a standard form, i.e. 
        ///     {4}*{5} => a*b
        /// Where
        ///     a is inputParameters[0],
        ///     b is inputParameters[1], etc.
        /// </summary>
        private object MathFormula(IReadOnlyList<SqlInputParameter> sqlInputParameters)
        {
            if (currentMathFormula != null) return currentMathFormula;
            inputParameters.Clear();
            var parameter = 'a';
            var formula = DesiredResultFormula;
            var index = 0;
            while (formula.Contains("{") && index < sqlInputParameters.Count)
            {
                var lookFor = string.Format("{{{0}}}", index++); // {0}, {1} etc.
                if (!formula.Contains(lookFor)) continue;
                formula = formula.Replace(lookFor, "" + parameter++);
                inputParameters.Add(sqlInputParameters[index-1]);
            }
            var engine = new CalculationEngine(CultureInfo.InvariantCulture);
            var formulaBuilder = engine.Formula(formula).Result(DataType.FloatingPoint);
            parameter = 'a';
            for (var i = 0; i < inputParameters.Count; i++)
                formulaBuilder.Parameter("" + parameter++, DataType.FloatingPoint);
            return currentMathFormula = formulaBuilder.Build();
        }

        private readonly List<SqlInputParameter> inputParameters = new List<SqlInputParameter>();

        /// <summary>
        /// Compute the desired result based on the formula
        /// </summary>
        /// <param name="poi"></param>
        /// <param name="sqlInputParameters"></param>
        /// <param name="updateMathFormula">If true, forces to re-evaluate the math formula.</param>
        /// <returns></returns>
        public double ComputeDesiredResult(BaseContent poi, List<SqlInputParameter> sqlInputParameters, bool updateMathFormula = false)
        {
            if (updateMathFormula) currentMathFormula = null;
            var mathFormula = MathFormula(sqlInputParameters);
            return CalculateResult(poi, mathFormula);
        }

        /// <summary>
        /// Calculate the results using the created formula.
        /// Note that you can have up to 8 parameters.
        /// </summary>
        /// <param name="bc"></param>
        /// <param name="mathFormula"></param>
        /// <returns>The computed result.</returns>
        private double CalculateResult(BaseContent bc, object mathFormula)
        {
            var parameters = new double[inputParameters.Count];
            for (var i = 0; i < parameters.Length; i++)
            {
                var sqlInputParameter = inputParameters[i];
                switch (sqlInputParameter.Type)
                {
                    case SqlParameterTypes.Label:
                        parameters[i] = bc.LabelToDouble(sqlInputParameter.LabelName);
                        break;
                    case SqlParameterTypes.Sensor:
                        parameters[i] = GetSensorValue(bc, sqlInputParameter.LabelName);
                        break;
                }
            }
            var result = 0d;
            switch (parameters.Length)
            {
                case 2:
                {
                    var formula = mathFormula as Func<double, double, double>;
                    result = formula == null ? 0 : formula(parameters[0], parameters[1]);
                    break;
                }
                case 3:
                {
                    var formula = mathFormula as Func<double, double, double, double>;
                    result = formula == null ? 0 : formula(parameters[0], parameters[1], parameters[2]);
                    break;
                }
                case 4:
                {
                    var formula = mathFormula as Func<double, double, double, double, double>;
                    result = formula == null ? 0 : formula(parameters[0], parameters[1], parameters[2], parameters[3]);
                    break;
                }
                case 5:
                {
                    var formula = mathFormula as Func<double, double, double, double, double, double>;
                    result = formula == null ? 0 : formula(parameters[0], parameters[1], parameters[2], parameters[3], parameters[4]);
                    break;
                }
                case 6:
                {
                    var formula = mathFormula as Func<double, double, double, double, double, double, double>;
                    result = formula == null ? 0 : formula(parameters[0], parameters[1], parameters[2], parameters[3], parameters[4], parameters[5]);
                    break;
                }
                case 7:
                {
                    var formula = mathFormula as Func<double, double, double, double, double, double, double, double>;
                    result = formula == null ? 0 : formula(parameters[0], parameters[1], parameters[2], parameters[3], parameters[4], parameters[5], parameters[6]);
                    break;
                }
                case 8:
                {
                    var formula = mathFormula as Func<double, double, double, double, double, double, double, double, double>;
                    result = formula == null ? 0 : formula(parameters[0], parameters[1], parameters[2], parameters[3], parameters[4], parameters[5], parameters[6], parameters[7]);
                    break;
                }
            }
            if (string.IsNullOrEmpty(DesiredResultLabel)) return result;
            bc.Labels[DesiredResultLabel] = result.ToString(CultureInfo.InvariantCulture);
            Caliburn.Micro.Execute.OnUIThread(() => bc.TriggerLabelChanged(DesiredResultLabel));
            return result;
        }

        ///// <summary>
        ///// Try to parse the label to a double using either the invariant or the Dutch culture.
        ///// </summary>
        ///// <param name="bc"></param>
        ///// <param name="key"></param>
        ///// <returns></returns>
        //private static double ParseLabelToDouble(BaseContent bc, string key)
        //{
        //    if (!bc.Labels.ContainsKey(key)) return 0;
        //    double fvalue;
        //    if (double.TryParse(bc.Labels[key], NumberStyles.Any, CultureInfo.InvariantCulture, out fvalue)) return fvalue;
        //    double.TryParse(bc.Labels[key], NumberStyles.Any, new CultureInfo("NL-nl"), out fvalue);
        //    return fvalue;
        //}

        private static double GetSensorValue(BaseContent bc, string key)
        {
            var fvalue = 0d;
            if (bc.Sensors.Count > 0 && bc.Sensors.ContainsKey(key))
                fvalue = bc.Sensors[key].FocusValue;
            return fvalue;
        }

        #endregion Non-serialized part that computes the desired result

    }
}