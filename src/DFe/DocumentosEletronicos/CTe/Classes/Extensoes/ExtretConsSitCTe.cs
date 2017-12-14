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
using System.IO;
using System.Text;
using DFe.Configuracao;
using DFe.DocumentosEletronicos.CTe.Classes.Retorno.Consulta;
using DFe.DocumentosEletronicos.ManipuladorDeXml;
using DFe.DocumentosEletronicos.ManipulaPasta;

namespace DFe.DocumentosEletronicos.CTe.Classes.Extensoes
{
    public static class ExtretConsSitCTe
    {
        /// <summary>
        ///     Coverte uma string XML no formato CTe para um objeto retConsSitCTe
        /// </summary>
        /// <param name="retConsSitCTe"></param>
        /// <param name="xmlString"></param>
        /// <returns>Retorna um objeto do tipo retConsSitNFe</returns>
        public static retConsSitCTe CarregarDeXmlString(this retConsSitCTe retConsSitCTe, string xmlString)
        {
            return FuncoesXml.XmlStringParaClasse<retConsSitCTe>(xmlString);
        }

        /// <summary>
        ///     Converte o objeto retConsSitCTe para uma string no formato XML
        /// </summary>
        /// <param name="retConsSitCTe"></param>
        /// <returns>Retorna uma string no formato XML com os dados do objeto retConsSitCTe</returns>
        public static string ObterXmlString(this retConsSitCTe retConsSitCTe)
        {
            return FuncoesXml.ClasseParaXmlString(retConsSitCTe);
        }

        public static void SalvarXmlEmDisco(this retConsSitCTe retConsSitCTe, string chave, DFeConfig config)
        {
            if (config.NaoSalvarXml()) return;

            var caminhoXml = new ResolvePasta(config, DateTime.Now).PastaConsultaProtocoloRetorno();

            var arquivoSalvar = Path.Combine(caminhoXml, new StringBuilder(chave).Append("-sit.xml").ToString());

            FuncoesXml.ClasseParaArquivoXml(retConsSitCTe, arquivoSalvar);
        }

        public static bool IsAutorizado(this retConsSitCTe retConsSitCTe)
        {
            const int autorizado = 100;
            return retConsSitCTe.cStat == autorizado; // manual cte 3.00 página 89
        }

        public static bool IsCancelada(this retConsSitCTe retConsSitCTe)
        {
            const int cancelada = 101;
            return retConsSitCTe.cStat == cancelada; // manual cte 3.00 página 89
        }

        public static bool IsDenegada(this retConsSitCTe retConsSitCTe)
        {
            const int denegada = 110;
            return retConsSitCTe.cStat == denegada; // manual cte 3.00 página 89
        }
    }
}