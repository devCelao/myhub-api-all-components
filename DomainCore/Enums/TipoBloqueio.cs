namespace DomainObjects.Enums;

public enum TipoBloqueio
{
    Nenhum = 0,
    TemporarioTentativas = 1,  // Auto por tentativas falhas
    ManualTemporario = 2,       // Admin bloqueou temporariamente
    ManualPermanente = 3,       // Admin bloqueou permanentemente
    SuspeitaFraude = 4,         // Sistema detectou atividade suspeita
    InativoPorTempo = 5         // Conta inativa por muito tempo
}

