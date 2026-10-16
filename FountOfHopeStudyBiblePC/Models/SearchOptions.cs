using FountOfHopeStudyBiblePC.Helpers;

namespace FountOfHopeStudyBiblePC.Models
{
    public class SearchOptions
    {
        public (int, int)? BookRange { get; set; }
        public string Scope { get; set; } = SearchScopes.Whole;

        public (int, int)? EffectiveBookRange
        {
            get
            {
                if (SearchScopes.IsBookScope(Scope))
                {
                    var n = SearchScopes.GetBookNumberFromScope(Scope);
                    return n.HasValue ? (n.Value, n.Value) : null;
                }
                return SearchScopes.ScopeRanges.TryGetValue(Scope, out var r)
                    ? r is null ? null : (r.Start, r.End)
                    : null;
            }
        }
    }
}