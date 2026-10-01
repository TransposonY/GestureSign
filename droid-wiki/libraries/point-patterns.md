# Point patterns
Active contributors: TransposonY

`GestureSign.PointPatterns` is a standalone .NET 10 Windows library for comparing a captured stroke against named candidate strokes. It resamples strokes, compares segment directions, and returns a numeric match score; capture, gesture persistence, multi-stroke coordination, and recognition thresholds belong to callers.

## Purpose and file layout

| Area | Contents |
| --- | --- |
| `GestureSign.PointPatterns/` | Pattern contract, candidate container, analyzer, math helpers, and match-result type |

## Key types

| Type | Role |
| --- | --- |
| `IPointPattern` (`GestureSign.PointPatterns/IPointPattern.cs`) | Contract exposing a pattern as an array of `System.Drawing.Point` strokes |
| `PointsPatternSet` (`GestureSign.PointPatterns/PointsPatternSet.cs`) | Carries a candidate name and one point sequence; lazily computes angular margins |
| `PointPatternAnalyzer` (`GestureSign.PointPatterns/PointPatternAnalyzer.cs`) | Compares input points to candidate sets at its configured precision (default 100) |
| `PointPatternMatchResult` (`GestureSign.PointPatterns/PointPatternMatchResult.cs`) | Carries a candidate name and numeric probability |
| `PointPatternMath` (`GestureSign.PointPatterns/PointPatternMath.cs`) | Provides interpolation, angular-margin/delta, distance, and score calculations |

## How comparison works

`PointPatternAnalyzer.GetPointPatternMatchResults` wraps the incoming stroke in a `PointsPatternSet` and compares it with each supplied candidate. For multi-point strokes, `PointsPatternSet.GetAngularMargins` interpolates the points and caches the resulting angular margins; the analyzer computes corresponding angular deltas and passes their average to `PointPatternMath.GetProbabilityFromAngularDelta`. A zero angular delta maps to a score of 100. The analyzer returns results in candidate enumeration order; callers decide if or how to rank and threshold them.

Single-point and degenerate input cases are handled separately by the analyzer using point counts. `IPointPattern` describes a pattern with multiple strokes, while this analyzer API compares one `Point[]` stroke at a time.

## Integration points

- `GestureSign.Common/Gestures/GestureManager.cs` builds candidate sets from saved gesture samples and uses this library during recognition.
- Common coordinates strokes and gestures, applies the configured recognition behavior, and owns persistence; this package only provides single-stroke calculations.
- `GestureSign.PointPatterns/GestureSign.PointPatterns.csproj` targets .NET 10 Windows and includes its sources automatically.

## Modification starting point

Adjust comparison flow or precision behavior in `GestureSign.PointPatterns/PointPatternAnalyzer.cs`; adjust resampling, angle-delta, distance, or score calculations in `GestureSign.PointPatterns/PointPatternMath.cs`. Update `GestureSign.PointPatterns/PointsPatternSet.cs` if candidate data or angular-margin caching changes, and keep `GestureSign.PointPatterns/IPointPattern.cs` aligned with any change to the pattern shape contract.

## Key source files

| Path | Responsibility |
| --- | --- |
| `GestureSign.PointPatterns/PointPatternAnalyzer.cs` | Per-candidate comparison and match result creation |
| `GestureSign.PointPatterns/PointPatternMath.cs` | Point interpolation, geometric helpers, and angular score calculation |
| `GestureSign.PointPatterns/PointsPatternSet.cs` | Candidate stroke/name storage and lazy angular margins |
| `GestureSign.PointPatterns/IPointPattern.cs` | Multi-stroke pattern contract |
| `GestureSign.PointPatterns/PointPatternMatchResult.cs` | Match result data shape |
| `GestureSign.PointPatterns/GestureSign.PointPatterns.csproj` | Framework target, references, and explicit compile list |
| `GestureSign.Common/Gestures/GestureManager.cs` | Gesture-level recognition caller |

## Related pages

- [Common recognition and managers](common.md)
- [Library overview](index.md)
- [Application architecture](../overview/architecture.md)
