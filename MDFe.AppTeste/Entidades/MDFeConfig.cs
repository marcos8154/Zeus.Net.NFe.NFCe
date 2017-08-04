﻿using DFe.Configuracao;
using DFe.DocumentosEletronicos.MDFe.Classes.Flags;
using DFe.Entidades;
using DFe.Flags;

namespace MDFe.AppTeste.Entidades
{
    public class MDFeConfig : DFeConfig
    {
        public override TipoAmbiente TipoAmbiente { get; set; }
        public override VersaoServico VersaoServico { get; set; }
        public override Estado EstadoUf { get; set; }
        public override string CnpjEmitente { get; set; }
    }
}