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
    public const string AlgorithmVersion = "3.0.0";

    /// <summary>
    /// Queries below which the areas based on classification are not
    /// measurable.
    /// </summary>
    /// <remarks>
    /// A network that contacted no tracker in three queries is not a protected
    /// network: it is a network that was not observed. Without this condition
    /// the least used network would obtain the best result, and the score
    /// would be measuring silence.
    /// </remarks>
    public const long MinimumQueriesForClassification = 100;

    private static readonly ThreatCategory[] PrivacyCategories =
    [
        ThreatCategory.Tracking,
        ThreatCategory.Analytics,
        ThreatCategory.Advertising,
    ];

    private static readonly ThreatCategory[] ConfirmedThreatCategories =
    [
        ThreatCategory.Malware,
        ThreatCategory.Phishing,
        ThreatCategory.Cryptomining,
    ];

    /// <summary>
    /// Coverage below which no overall score is produced.
    /// </summary>
    /// <remarks>
    /// Below this level the judgement would rest on less than three fifths of
    /// the evaluation system, and a single figure would communicate a
    /// completeness that does not exist.
    /// <para>
    /// The breakdown is produced in any case: the system holds valid
    /// measurements and is declining only the summary.
    /// </para>
    /// </remarks>
    public const decimal MinimumCoverageForOverallScore = 60;

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
            EvaluatePrivacyProtection(input),
            EvaluateThreatProtection(input),
            NotMeasurable(
                ScoreComponentType.DeviceHealth,
                weight: 15,
                "Richiede il Device Engine e l'Alert Engine, non ancora implementati."),
            EvaluateConfiguration(input),
            EvaluateNetworkIntegrity(input),
        ];

        decimal coverage = breakdown.Sum(component => component.MaxScore);
        decimal obtained = breakdown.Sum(component => component.Score);

        // Below the minimum coverage the summary is not produced. Nothing
        // takes its place: no provisional figure, no zero, no placeholder.
        bool summaryIsSupportable = coverage >= MinimumCoverageForOverallScore;

        decimal? overall = summaryIsSupportable
            ? Math.Round(obtained / coverage * 100, MidpointRounding.AwayFromZero)
            : null;

        return new Npss
        {
            OverallScore = overall is null ? null : (int)overall.Value,
            Status = overall is null ? null : ResolveStatus(overall.Value),
            Trend = overall is null ? null : ResolveTrend(input, overall.Value, coverage),
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

    /// <summary>
    /// Evaluates how much the network is tracked and how much of it is stopped.
    /// </summary>
    /// <remarks>
    /// The two indicators answer two questions the person asks separately.
    /// Blocking alone would reward an effective filter on a besieged network;
    /// exposure alone would ignore the work the filter does.
    /// </remarks>
    private static ScoreComponent EvaluatePrivacyProtection(NpssEvaluationInput input)
    {
        const int Weight = 20;

        if (Unobservable(input) is string obstacle)
        {
            return NotMeasurable(ScoreComponentType.PrivacyProtection, Weight, obstacle);
        }

        decimal score = 0;
        decimal maxScore = 10;
        List<string> factors = [];

        // Known Tracking Exposure, 10 points.
        //
        // Counted per query and not per domain: ten domains contacted once
        // each and one domain contacted four hundred times describe different
        // networks, and counting domains would make them look alike.
        long trackingQueries = QueriesTowards(input, PrivacyCategories);
        decimal share = (decimal)trackingQueries / input.Statistics.TotalQueries;

        score += share switch
        {
            0 => 10,
            <= 0.02m => 8,
            <= 0.05m => 6,
            <= 0.10m => 4,
            <= 0.20m => 2,
            _ => 0,
        };

        // The wording carries the asymmetry the specification states: the
        // lists assert that a domain tracks, never that it does not.
        factors.Add(trackingQueries == 0
            ? "Nessun tracciamento noto osservato. I domini non presenti in alcuna lista restano non classificati, quindi il valore è un limite inferiore."
            : $"Interrogazioni verso domini noti di tracciamento, pubblicità o analisi: {Percent(share)} del totale. Il valore è un limite inferiore.");

        // Tracking Blocking, 10 points.
        BlockingOutcome blocking = MeasureBlocking(
            input,
            PrivacyCategories,
            points: 10,
            thresholds: [(0.99m, 10), (0.90m, 8), (0.75m, 6), (0.50m, 4), (0.25m, 2)],
            nothingToBlock: "Nessuna interrogazione verso domini di tracciamento noti: il filtro non è stato messo alla prova.",
            subject: "tracciamento");

        score += blocking.Score;
        maxScore += blocking.MaxScore;
        factors.Add(blocking.Factor);

        return Build(ScoreComponentType.PrivacyProtection, Weight, score, maxScore, factors);
    }

    /// <summary>
    /// Evaluates the known threats reaching the network and how many are
    /// stopped.
    /// </summary>
    /// <remarks>
    /// Exposure is counted per domain rather than as a share. A malware domain
    /// contacted once is a fact worth reporting, and diluting it over the total
    /// number of queries would make it disappear.
    /// </remarks>
    private static ScoreComponent EvaluateThreatProtection(NpssEvaluationInput input)
    {
        const int Weight = 25;

        if (Unobservable(input) is string obstacle)
        {
            return NotMeasurable(ScoreComponentType.ThreatProtection, Weight, obstacle);
        }

        decimal score = 0;
        decimal maxScore = 12;
        List<string> factors = [];

        int confirmed = input.Domains.Count(
            domain => ConfirmedThreatCategories.Contains(domain.Category));

        int suspicious = input.Domains.Count(
            domain => domain.Category == ThreatCategory.Suspicious);

        // Known Threat Exposure, 12 points.
        score += (confirmed, suspicious) switch
        {
            (0, 0) => 12,

            // A suspicious domain lowers the score without emptying it. The
            // report is not confirmed, and treating it as an established
            // threat would attribute to the network a problem never shown.
            (0, _) => 9,
            (1, _) => 6,
            (<= 5, _) => 3,
            _ => 0,
        };

        factors.Add((confirmed, suspicious) switch
        {
            (0, 0) => "Nessuna minaccia nota e nessun dominio sospetto osservato. Il valore è un limite inferiore: le liste affermano ciò che riconoscono.",
            (0, _) => $"Nessuna minaccia confermata. Domini segnalati come sospetti: {suspicious}. La segnalazione non è confermata.",
            _ => $"Domini di minaccia confermata osservati: {confirmed}. Domini sospetti: {suspicious}.",
        });

        // Threat Blocking, 13 points. The bar is higher than for tracking: a
        // tracker that gets through costs privacy, a malware domain that gets
        // through can cost the machine.
        BlockingOutcome blocking = MeasureBlocking(
            input,
            [.. ConfirmedThreatCategories, ThreatCategory.Suspicious],
            points: 13,
            thresholds: [(1m, 13), (0.95m, 10), (0.80m, 6), (0.50m, 3)],
            nothingToBlock: "Nessuna interrogazione verso domini di minaccia noti: il filtro non è stato messo alla prova.",
            subject: "minaccia");

        score += blocking.Score;
        maxScore += blocking.MaxScore;
        factors.Add(blocking.Factor);

        return Build(ScoreComponentType.ThreatProtection, Weight, score, maxScore, factors);
    }

    /// <summary>
    /// Returns why the areas based on classification cannot be measured, or
    /// null when they can.
    /// </summary>
    private static string? Unobservable(NpssEvaluationInput input)
    {
        if (!input.ClassificationAvailable)
        {
            return "Nessuna lista di classificazione disponibile: senza liste ogni dominio risulta non classificato, e leggerlo come assenza di tracciamento trasformerebbe la mancanza di uno strumento in un buon risultato.";
        }

        if (input.Statistics.TotalQueries < MinimumQueriesForClassification)
        {
            return $"Interrogazioni osservate nel periodo insufficienti: {input.Statistics.TotalQueries} su {MinimumQueriesForClassification} richieste. Una rete poco osservata non è una rete protetta.";
        }

        return null;
    }

    private static long QueriesTowards(NpssEvaluationInput input, ThreatCategory[] categories)
    {
        return input.Domains
            .Where(domain => categories.Contains(domain.Category))
            .Sum(domain => domain.Occurrences);
    }

    /// <summary>
    /// Result of the blocking indicator of an area.
    /// </summary>
    /// <param name="Score">Points obtained.</param>
    /// <param name="MaxScore">Points that were obtainable, zero when the
    /// indicator could not be measured.</param>
    /// <param name="Factor">What to tell the person.</param>
    private readonly record struct BlockingOutcome(decimal Score, decimal MaxScore, string Factor);

    /// <summary>
    /// Measures how much of the traffic towards a set of categories was
    /// blocked.
    /// </summary>
    private static BlockingOutcome MeasureBlocking(
        NpssEvaluationInput input,
        ThreatCategory[] categories,
        decimal points,
        (decimal Threshold, decimal Points)[] thresholds,
        string nothingToBlock,
        string subject)
    {
        if (!input.DomainActivityAvailable)
        {
            return new BlockingOutcome(
                0,
                0,
                $"La sorgente non riporta l'attività per dominio: la quota di {subject} bloccata non è valutabile.");
        }

        HashSet<string> names =
        [
            .. input.Domains
                .Where(domain => categories.Contains(domain.Category))
                .Select(domain => domain.Name),
        ];

        List<DomainActivity> relevant =
        [
            .. input.DomainActivities.Where(activity => names.Contains(activity.Domain)),
        ];

        long total = relevant.Sum(activity => activity.QueryCount);

        if (total == 0)
        {
            // Excluded rather than scored: with nothing to block there is no
            // judgement to pass, and the full marks already came from the
            // exposure indicator.
            return new BlockingOutcome(0, 0, nothingToBlock);
        }

        long blocked = relevant.Where(activity => activity.Blocked).Sum(activity => activity.QueryCount);
        decimal blockedShare = (decimal)blocked / total;

        decimal obtained = 0;

        foreach ((decimal threshold, decimal awarded) in thresholds)
        {
            if (blockedShare >= threshold)
            {
                obtained = awarded;
                break;
            }
        }

        return new BlockingOutcome(
            obtained,
            points,
            $"Interrogazioni verso domini di {subject} bloccate: {Percent(blockedShare)} su {total}.");
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
