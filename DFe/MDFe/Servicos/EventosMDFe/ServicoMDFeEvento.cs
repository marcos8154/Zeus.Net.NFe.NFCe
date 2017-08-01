﻿/********************************************************************************/
/* Projeto: Biblioteca ZeusMDFe                                                 */
/* Biblioteca C# para emissão de Manifesto Eletrônico Fiscal de Documentos      */
/* (https://mdfe-portal.sefaz.rs.gov.br/                                        */
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

using DFe.MDFe.Classes.Retorno.MDFeEvento;

namespace DFe.MDFe.Servicos.EventosMDFe
{
    public class ServicoMDFeEvento
    {
        public MDFeRetEventoMDFe MDFeEventoIncluirCondutor(
            string chave, string cnpj, byte sequenciaEvento, string nome,
            string cpf)
        {
            var eventoIncluirCondutor = new MDFeIncluirCondutor();

            return eventoIncluirCondutor.MDFeEventoIncluirCondutor(chave, cnpj, sequenciaEvento, nome, cpf);
        }

        public MDFeRetEventoMDFe MDFeEventoEncerramentoMDFeEventoEncerramento(string chave, string cnpj, long codigoIbgeCidade, byte sequenciaEvento, string protocolo)
        {
            var eventoEncerramento = new MDFeEncerrar();

            return eventoEncerramento.MDFeEventoEncerramento(chave, cnpj, codigoIbgeCidade, sequenciaEvento, protocolo);
        }

        public MDFeRetEventoMDFe MDFeEventoCancelar(string chave, string cnpj, byte sequenciaEvento, string protocolo,
            string justificativa)
        {
            var eventoCancelamento = new MDFeCancelar();

            return eventoCancelamento.Cancelar(chave, cnpj, sequenciaEvento, protocolo, justificativa);
        }
    }
}