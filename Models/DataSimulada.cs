using Banksim_Web.Models;
using Banksim_Web.Enums;
using System;

public class DataSimulada
{
    // Chave primária
    public int Id { get; set; }

    // Chave estrangeira
    public int ContaBancariaID { get; set; }
    public ContaBancaria ContaBancaria { get; set; }

    // Datas de referência
    public int DiaInicial { get; set; }
    public int MesInicial { get; set; } // Armazena como int (1-12)
    public int AnoInicial { get; set; }

    public int DiaAtual { get; set; }
    public int MesAtual { get; set; }
    public int AnoAtual { get; set; }

    // Construtor com valores iniciais
    public DataSimulada(int dia, int mes)
    {
        DiaInicial = DiaAtual = dia;
        MesInicial = MesAtual = mes;
        AnoInicial = AnoAtual = DateTime.Now.Year;
    }

    // Construtor vazio exigido pelo EF
    public DataSimulada() { }

    // Método para obter quantos dias há no mês (considera anos bissextos)
    private int ObterDiasNoMes(int mes, int ano)
    {
        return DateTime.DaysInMonth(ano, mes);
    }

    // Avança um número específico de dias, respeitando meses e anos
    public void AvancarDias(int quantidade)
    {
        for (int i = 0; i < quantidade; i++)
        {
            DiaAtual++;
            if (DiaAtual > ObterDiasNoMes(MesAtual, AnoAtual))
            {
                DiaAtual = 1;
                MesAtual++;

                if (MesAtual > 12)
                {
                    MesAtual = 1;
                    AnoAtual++;
                }
            }
        }
    }

    // Reseta o calendário mantendo o ano atual do sistema
    public void Resetar(int novoDia, int novoMes)
    {
        DiaInicial = DiaAtual = novoDia;
        MesInicial = MesAtual = novoMes;
        AnoInicial = AnoAtual = DateTime.Now.Year;
    }

    public override string ToString()
    {
        return $"{DiaAtual:D2}/{MesAtual:D2}/{AnoAtual}";
    }

    // Retorna o nome do mês atual usando o enum
    public string NomeMesAtual => Enum.GetName(typeof(Meses), MesAtual);
}