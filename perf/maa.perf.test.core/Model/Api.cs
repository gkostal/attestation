namespace maa.perf.test.core.Model
{
    public enum Api
    {
        None,
        // Attest
        AttestSgxEnclave,       // Preview, GA
        AttestTeeSgxEnclave,    // Preview
        AttestTeeOpenEnclave,   // Preview
        AttestTeeSevSnpVm,      // Preview
        AttestTeeAzureGuest,    // Preview
        AttestOpenEnclave,      // GA
        AttestSevSnpVm,         // GA
        AttestSevSnpVmUvm,      // GA
        AttestAzureGuest,       // GA
        // JWT validation
        GetOpenIdConfiguration, // Preview, GA
        GetCerts,               // Preview, GA
        // Other
        GetServiceHealth,       // GA
        GetPolicy,              // GA
        SetPolicy,              // GA
    };
}
