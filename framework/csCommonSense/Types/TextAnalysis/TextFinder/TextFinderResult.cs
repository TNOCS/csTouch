namespace csCommon.Types.TextAnalysis.TextFinder
{
    /// <summary>
    /// The result of a text finding operation.
    /// </summary>
    /// <typeparam name="TS"></typeparam>
    public class TextFinderResult<TS> where TS : ITextSearchable
    {
        /// <summary>
        /// The data returned by the finding operation.
        /// </summary>
        public TS Data { get; set; }

        /// <summary>
        /// The score of this data, which generally should be the number of occurrences of the search query.
        /// </summary>
        public double Score { get; set; }

        /// <summary>
        /// Construct a text finder result.
        /// </summary>
        /// <param name="data">The data to return.</param>
        /// <param name="score">The score to return.</param>
        internal TextFinderResult(TS data, double score)
        {
            Data = data;
            Score = score;
        }
    }
}
