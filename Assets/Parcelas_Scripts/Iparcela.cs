public interface IParcela
{
    bool InteractuarSlot(int indice, string herramienta);
}

/// <summary>
/// Permite que la interaccion busque el slot correcto para cada herramienta
/// sin cambiar el contrato original de IParcela.
/// </summary>
public interface IParcelaConsultable
{
    bool PuedeInteractuarSlot(int indice, string herramienta);
    int CultivosPendientes { get; }
}
