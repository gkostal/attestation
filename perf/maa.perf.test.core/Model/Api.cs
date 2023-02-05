namespace maa.perf.test.core.Model
{
    public enum Api
    {
        None,
        // Attest
        AttestSgxEnclave,       // Preview, GA
        AttestVsmEnclave,       // Preview
        AttestVbsEnclave,       // Preview
        AttestTeeSgxEnclave,    // Preview
        AttestTeeOpenEnclave,   // Preview
        AttestTeeVsmEnclave,    // Preview
        AttestTeeVbsEnclave,    // Preview
        AttestOpenEnclave,      // GA
        AttestSevSnpVm,         // GA
        AttestTpm,              // GA
        // JWT validation
        GetOpenIdConfiguration, // Preview, GA
        GetCerts,               // Preview, GA
        // Other
        GetServiceHealth        // GA
    };
}
