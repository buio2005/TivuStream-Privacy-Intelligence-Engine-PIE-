using TivuStream.Pie.Model.Entities;
using TivuStream.Pie.Model.Enums;

namespace TivuStream.Pie.Core;

/// <summary>
/// Computes the Network Privacy and Security Score.
/// </summary>
/// <remarks>
/// Every indicator follows the definition given in the NPSS Specification.
/// Nothing is decided here that is not written there.
/// <para>
/// What was not observed is excluded from the calculation: it is never scored
/// as zero, and never assumed favourable. Missing data can therefore neither
/// penalise nor improve the result.
/// </para>
/// </remarks>
public sealed class NpssEngine
{
    /// <summary>
    /// Version of the scoring algorithm.
    /// </summary>
    /// <remarks>
    /// Independent of the version of the project, as the specification
    /// requires. Scores produced by different versions are not comparable.
    /// </remarks>
    public const string AlgorithmVersion = "1.0.0";

    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Creates the engine.
    /// </summary>
    /// <param name="timeProvider">Source of the current instant.</param>
    /// <remarks>
    /// The instant is taken from a provider rather than from the system clock
    /// so that an evaluation can be reproduced at a chosen moment. An engine
    /// that reads the clock directly cannot be verified.
    /// </remarks>
    public NpssEngine(TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);

        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Evaluates the score.
    /// </summary>
    /// <param name="input">Data the evaluation is based on.</param>
    public Npss Evaluate(NpssEvaluationInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        List<ScoreComponent> breakdown =
        [
            EvaluateDnsSecurity(input),
            NotMeasurable(
                ScoreComponentType.PrivacyProtection,
                weight: 20,
                "Richiede la classificazione dei domini, non ancora disponibile."),
            NotMeasurable(
                ScoreComponentType.ThreatProtection,
                weight: 25,
                "Richiede la classificazione dei domini, non ancora disponibile."),
            NotMeasurable(
                ScoreComponentType.DeviceHealth,
                weight: 15,
                "Richiede il Device Engine e l'Alert Engine, non ancora implementati."),
            EvaluateConfiguration(input),
            EvaluateNetworkIntegrity(input),
        ];

        decimal coverage = breakdown.Sum(component => component.MaxScore);
        decimal obtained = breakdown.Sum(component => component.Score);

        decimal overall = coverage > 0
            ? Math.Round(obtained / coverage * 100, MidpointRounding.AwayFromZero)
            : 0;

        return new Npss
        {
            OverallScore = (int)overall,
            Status = ResolveStatus(overall),
            Trend = ResolveTrend(input, overall, coverage),
            Coverage = coverage,
            AlgorithmVersion = AlgorithmVersion,
            GeneratedAt = _timeProvider.GetUtcNow(),
            Breakdown = breakdown,
        };
    }

    private static ScoreComponent EvaluateDnsSecurity(NpssEvaluationInput input)
    {
        Statistics statistics = input.Statistics;
        SourceConfiguration? configuration = input.Configuration;

        decimal score = 0;
        decimal maxScore = 0;
        List<string> factors = [];

        bool hasTraffic = statistics.TotalQueries > 0;

        if (configuration is null)
        {
            factors.Add("Configurazione della sorgente non disponibile: validazione DNSSEC, trasporti cifrati e impostazioni del resolver non valutabili.");
        }
        else
        {
            // DNSSEC Validation, 5 points.
            maxScore += 5;

            if (configuration.DnssecValidationEnabled)
            {
                score += 5;
                factors.Add("Validazione DNSSEC attiva.");
            }
            else
            {
                factors.Add("Validazione DNSSEC non attiva.");
            }

            // Transport Encryption, 2 points for availability.
            maxScore += 2;

            if (configuration.EncryptedTransports.Count > 0)
            {
                score += 2;
                factors.Add($"Trasporti cifrati disponibili: {string.Join(", ", configuration.EncryptedTransports)}.");
            }
            else
            {
                factors.Add("Nessun trasporto cifrato abilitato.");
            }

            // Resolver Configuration, 2.5 points each.
            maxScore += 5;

            if (configuration.QueryMinimisationEnabled)
            {
                score += 2.5m;
                factors.Add("Minimizzazione del nome interrogato attiva.");
            }
            else
            {
                factors.Add("Minimizzazione del nome interrogato non attiva.");
            }

            if (configuration.ClientSubnetForwardingEnabled)
            {
                factors.Add("Inoltro della sottorete del client attivo: riduce la privacy verso i server esterni.");
            }
            else
            {
                score += 2.5m;
                factors.Add("Inoltro della sottorete del client disattivato.");
            }
        }

        // Transport Encryption, 3 points for actual use.
        if (hasTraffic)
        {
            maxScore += 3;

            decimal encryptedShare = (decimal)statistics.EncryptedQueries / statistics.TotalQueries;
            score += 3 * encryptedShare;

            factors.Add($"Interrogazioni ricevute su trasporto cifrato: {Percent(encryptedShare)}.");

            // DNS Errors, 5 points.
            maxScore += 5;

            decimal failureShare = Math.Min(1, (decimal)statistics.FailedQueries / statistics.TotalQueries);
            score += 5 * (1 - failureShare);

            factors.Add($"Interrogazioni non soddisfatte: {Percent(failureShare)}.");
        }
        else
        {
            factors.Add("Nessun traffico osservato nel periodo: utilizzo dei trasporti cifrati ed errori non valutabili.");
        }

        return Build(ScoreComponentType.DnsSecurity, weight: 20, score, maxScore, factors);
    }

    private static ScoreComponent EvaluateConfiguration(NpssEvaluationInput input)
    {
        decimal score = 0;
        decimal maxScore = 5;
        List<string> factors = [];

        // Source Availability, 5 points.
        if (input.SourceReachable)
        {
            score += 5;
            factors.Add("La sorgente ha risposto correttamente.");
        }
        else
        {
            factors.Add("La sorgente non è raggiungibile.");
        }

        // Filtering Configuration, 2.5 points each.
        if (input.Configuration is null)
        {
            factors.Add("Configurazione della sorgente non disponibile: filtraggio non valutabile.");
        }
        else
        {
            maxScore += 5;

            if (input.Configuration.FilteringEnabled)
            {
                score += 2.5m;
                factors.Add("Filtraggio dei domini attivo.");
            }
            else
            {
                factors.Add("Filtraggio dei domini non attivo.");
            }

            if (input.Configuration.FilterListCount > 0)
            {
                score += 2.5m;
                factors.Add($"Liste di filtro configurate: {input.Configuration.FilterListCount}.");
            }
            else
            {
                factors.Add("Nessuna lista di filtro configurata: il filtraggio non ha effetto.");
            }
        }

        return Build(ScoreComponentType.Configuration, weight: 10, score, maxScore, factors);
    }

    private static ScoreComponent EvaluateNetworkIntegrity(NpssEvaluationInput input)
    {
        List<string> factors = [];

        // Observation Continuity, 5 points.
        decimal maxScore = 5;
        decimal score;

        if (input.ExpectedPeriods > 0)
        {
            decimal continuity = Math.Min(1, (decimal)input.ObservedPeriods / input.ExpectedPeriods);
            score = 5 * continuity;

            factors.Add($"Periodi osservati: {input.ObservedPeriods} su {input.ExpectedPeriods} attesi.");
        }
        else
        {
            score = 0;
            maxScore = 0;
            factors.Add("Nessun periodo atteso su cui valutare la continuità.");
        }

        // Acquisition Reliability is not measurable until acquisition
        // attempts, including the failed ones, are recorded.
        factors.Add("Affidabilità dell'acquisizione non valutabile: i tentativi non vengono ancora registrati.");

        return Build(ScoreComponentType.NetworkIntegrity, weight: 10, score, maxScore, factors);
    }

    private static ScoreComponent NotMeasurable(ScoreComponentType component, int weight, string reason)
    {
        return new ScoreComponent
        {
            Component = component,
            State = ScoreComponentState.NotMeasurable,
            Score = 0,
            MaxScore = 0,
            Weight = weight,
            Factors = [reason],
        };
    }

    private static ScoreComponent Build(
        ScoreComponentType component,
        int weight,
        decimal score,
        decimal maxScore,
        List<string> factors)
    {
        ScoreComponentState state = maxScore switch
        {
            0 => ScoreComponentState.NotMeasurable,
            _ when maxScore >= weight => ScoreComponentState.Measured,
            _ => ScoreComponentState.PartiallyMeasured,
        };

        return new ScoreComponent
        {
            Component = component,
            State = state,
            Score = Math.Round(score, 2, MidpointRounding.AwayFromZero),
            MaxScore = Math.Round(maxScore, 2, MidpointRounding.AwayFromZero),
            Weight = weight,
            Factors = factors,
        };
    }

    private static ScoreStatus ResolveStatus(decimal overall)
    {
        return overall switch
        {
            >= 90 => ScoreStatus.Excellent,
            >= 75 => ScoreStatus.Good,
            >= 60 => ScoreStatus.Fair,
            >= 40 => ScoreStatus.Warning,
            _ => ScoreStatus.Critical,
        };
    }

    private static ScoreTrend? ResolveTrend(NpssEvaluationInput input, decimal overall, decimal coverage)
    {
        // A change in coverage interrupts the series: two scores computed over
        // different portions of the evaluation system are not comparable.
        if (input.PreviousOverallScore is null || input.PreviousCoverage != coverage)
        {
            return null;
        }

        decimal difference = overall - input.PreviousOverallScore.Value;

        return difference switch
        {
            > 0 => ScoreTrend.Improving,
            < 0 => ScoreTrend.Decreasing,
            _ => ScoreTrend.Stable,
        };
    }

    private static string Percent(decimal share)
    {
        return $"{Math.Round(share * 100, 1, MidpointRounding.AwayFromZero)}%";
    }
}
