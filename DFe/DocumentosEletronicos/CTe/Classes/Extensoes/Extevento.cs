﻿/********************************************************************************/
/* Projeto: Biblioteca ZeusNFe                                                  */
/* Biblioteca C# para emissão de Nota Fiscal Eletrônica - NFe e Nota Fiscal de  */
/* Consumidor Eletrônica - NFC-e (http://www.nfe.fazenda.gov.br)                */
/*                                                                              */
/* Direitos Autorais Reservados (c) 2014 Adenilton Batista da Silva             */
/*                                       Zeusdev Tecnologia LTDA ME             */
/*                                                                              */
/*  Você pode obter a última versão desse arquivo no GitHub                     */
/* localizado em https://github.com/adeniltonbs/Zeus.Net.NFe.NFCe               */
/*                                                                              */
/*                                                                              */
/*  Esta biblioteca é software livre; você pode redistribuí-la e/ou modificá-la */
/* sob os termos da Licença Pública Geral Menor do GNU conforme publicada pela  */
/* Free Software Foundation; tanto a versão 2.1 da Licença, ou (a seu critério) */
/* qualquer versão posterior.                                                   */
/*                                                                              */
/*  Esta biblioteca é distribuída na expectativa de que seja útil, porém, SEM   */
/* NENHUMA GARANTIA; nem mesmo a garantia implícita de COMERCIABILIDADE OU      */
/* ADEQUAÇÃO A UMA FINALIDADE ESPECÍFICA. Consulte a Licença Pública Geral Menor*/
/* do GNU para mais detalhes. (Arquivo LICENÇA.TXT ou LICENSE.TXT)              */
/*                                                                              */
/*  Você deve ter recebido uma cópia da Licença Pública Geral Menor do GNU junto*/
/* com esta biblioteca; se não, escreva para a Free Software Foundation, Inc.,  */
/* no endereço 59 Temple Street, Suite 330, Boston, MA 02111-1307 USA.          */
/* Você também pode obter uma copia da licença em:                              */
/* http://www.opensource.org/licenses/lgpl-license.php                          */
/*                                                                              */
/* Zeusdev Tecnologia LTDA ME - adenilton@zeusautomacao.com.br                  */
/* http://www.zeusautomacao.com.br/                                             */
/* Rua Comendador Francisco josé da Cunha, 111 - Itabaiana - SE - 49500-000     */
/********************************************************************************/

using System;
using System.Diagnostics;
using System.Xml;
using DFe.Assinatura;
using DFe.CertificadosDigitais;
using DFe.Configuracao;
using DFe.DocumentosEletronicos.CTe.Classes.Flags;
using DFe.DocumentosEletronicos.CTe.Classes.Servicos.Evento;
using DFe.DocumentosEletronicos.CTe.Classes.Servicos.Evento.CorpoEvento;
using DFe.DocumentosEletronicos.CTe.Validacao;
using DFe.Flags;
using DFe.ManipuladorDeXml;
using DFe.ResolvePastas;

namespace DFe.DocumentosEletronicos.CTe.Classes.Extensoes
{
    public static class Extevento
    {
        /// <summary>
        ///     Converte o objeto evento para uma string no formato XML
        /// </summary>
        /// <param name="pedEventoCTe"></param>
        /// <returns>Retorna uma string no formato XML com os dados do objeto evento</returns>
        public static string ObterXmlString(this eventoCTe pedEventoCTe)
        {
            return FuncoesXml.ClasseParaXmlString(pedEventoCTe);
        }

        /// <summary>
        ///     Assina um objeto evento
        /// </summary>
        /// <param name="eventoCTe"></param>
        /// <returns>Retorna um objeto do tipo evento assinado</returns>
        public static void Assina(this eventoCTe eventoCTe, CertificadoDigital certificadoDigital)
        {
            if (eventoCTe.infEvento.Id == null)
                throw new Exception("Não é possível assinar um objeto evento sem sua respectiva Id!");

            eventoCTe.Signature = AssinaturaDigital.Assina(eventoCTe, eventoCTe.infEvento.Id,
                certificadoDigital);
        }

        public static void ValidarSchema(this eventoCTe eventoCTe, DFeConfig config)
        {
            var xmlEvento = eventoCTe.ObterXmlString();

            switch (eventoCTe.versao)
            {
                case VersaoServico.Versao200:
                    Validador.Valida(xmlEvento, "eventoCTe_v2.00.xsd", config);
                    break;
                case VersaoServico.Versao300:
                    Validador.Valida(xmlEvento, "eventoCTe_v3.00.xsd", config);
                    break;
                default:
                    throw new InvalidOperationException("Nos achamos um erro na hora de validar o schema, " +
                                                   "a versão está inválida, somente é permitido " +
                                                   "versão 2.00 é 3.00");
            }

            ValidarSchemaEventoContainer(eventoCTe.infEvento.detEvento.EventoContainer, eventoCTe.versao, config);
        }

        private static void ValidarSchemaEventoContainer(EventoContainer container, VersaoServico versao, DFeConfig config)
        {
            if (container.GetType() == typeof(evCancCTe))
            {
                var evCancCTe = (evCancCTe) container;

                var xmlEventoCancelamento = evCancCTe.ObterXmlString();

                switch (versao)
                {
                    case VersaoServico.Versao200:
                        Validador.Valida(xmlEventoCancelamento, "evCancCTe_v2.00.xsd", config);
                        break;
                    case VersaoServico.Versao300:
                        Validador.Valida(xmlEventoCancelamento, "evCancCTe_v3.00.xsd", config);
                        break;
                    default:
                        throw new InvalidOperationException("Nos achamos um erro na hora de validar o schema, " +
                                                       "a versão está inválida, somente é permitido " +
                                                       "versão 2.00 é 3.00");
                }


            }

            if (container.GetType() == typeof(evCCeCTe))
            {
                var evCCeCTe = (evCCeCTe)container;

                var xmlEventoCCe = evCCeCTe.ObterXmlString();

                switch (versao)
                {
                    case VersaoServico.Versao200:
                        Validador.Valida(xmlEventoCCe, "evCCeCTe_v2.00.xsd", config);
                        break;
                    case VersaoServico.Versao300:
                        Validador.Valida(xmlEventoCCe, "evCCeCTe_v3.00.xsd", config);
                        break;
                    default:
                        throw new InvalidOperationException("Nos achamos um erro na hora de validar o schema, " +
                                                       "a versão está inválida, somente é permitido " +
                                                       "versão 2.00 é 3.00");
                }
            }
        }

        public static void SalvarXmlEmDisco(this eventoCTe eventoCTe, DFeConfig config, TipoEvento tipoEvento)
        {
            if (config.NaoSalvarXml()) return;

            var caminhoXml = string.Empty;

            switch (tipoEvento)
            {
                case TipoEvento.Cancelamento:
                    caminhoXml = new ResolvePasta(config, eventoCTe.infEvento.dhEvento).PastaCanceladosEnvio();
                    break;
                case TipoEvento.CartaCorrecao:
                    caminhoXml = new ResolvePasta(config, eventoCTe.infEvento.dhEvento).PastaCartaCorrecaoEnvio();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(tipoEvento), tipoEvento, null);
            }

            var arquivoSalvar = caminhoXml + @"\" + eventoCTe.infEvento.chCTe + "-ped-eve.xml";

            FuncoesXml.ClasseParaArquivoXml(eventoCTe, arquivoSalvar);
        }

        public static XmlDocument CriaXmlRequestWs(this eventoCTe eventoCTe)
        {
            var xmlRequest = new XmlDocument();
            xmlRequest.LoadXml(eventoCTe.ObterXmlString());

            return xmlRequest;
        }
    }
}