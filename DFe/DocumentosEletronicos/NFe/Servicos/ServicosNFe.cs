﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Xml;
using DFe.Compressoes;
using DFe.DocumentosEletronicos.Entidades;
using DFe.DocumentosEletronicos.Flags;
using DFe.DocumentosEletronicos.NFe.Classes.Extensoes;
using DFe.DocumentosEletronicos.NFe.Classes.Informacoes.Identificacao.Tipos;
using DFe.DocumentosEletronicos.NFe.Classes.Retorno.AdmCsc;
using DFe.DocumentosEletronicos.NFe.Classes.Retorno.Autorizacao;
using DFe.DocumentosEletronicos.NFe.Classes.Retorno.Consulta;
using DFe.DocumentosEletronicos.NFe.Classes.Retorno.ConsultaCadastro;
using DFe.DocumentosEletronicos.NFe.Classes.Retorno.DistribuicaoDFe;
using DFe.DocumentosEletronicos.NFe.Classes.Retorno.Download;
using DFe.DocumentosEletronicos.NFe.Classes.Retorno.Evento;
using DFe.DocumentosEletronicos.NFe.Classes.Retorno.Inutilizacao;
using DFe.DocumentosEletronicos.NFe.Classes.Retorno.Recepcao;
using DFe.DocumentosEletronicos.NFe.Classes.Retorno.Status;
using DFe.DocumentosEletronicos.NFe.Classes.Servicos.AdmCsc;
using DFe.DocumentosEletronicos.NFe.Classes.Servicos.Autorizacao;
using DFe.DocumentosEletronicos.NFe.Classes.Servicos.Consulta;
using DFe.DocumentosEletronicos.NFe.Classes.Servicos.ConsultaCadastro;
using DFe.DocumentosEletronicos.NFe.Classes.Servicos.DistribuicaoDFe;
using DFe.DocumentosEletronicos.NFe.Classes.Servicos.Download;
using DFe.DocumentosEletronicos.NFe.Classes.Servicos.Evento;
using DFe.DocumentosEletronicos.NFe.Classes.Servicos.Inutilizacao;
using DFe.DocumentosEletronicos.NFe.Classes.Servicos.Recepcao;
using DFe.DocumentosEletronicos.NFe.Classes.Servicos.Status;
using DFe.DocumentosEletronicos.NFe.Excecoes;
using DFe.DocumentosEletronicos.NFe.Flags;
using DFe.DocumentosEletronicos.NFe.Utils;
using DFe.DocumentosEletronicos.NFe.Validacao;
using DFe.DocumentosEletronicos.NFe.Wsdl;
using DFe.DocumentosEletronicos.NFe.Wsdl.AdmCsc;
using DFe.DocumentosEletronicos.NFe.Wsdl.Autorizacao;
using DFe.DocumentosEletronicos.NFe.Wsdl.ConsultaCadastro.CE;
using DFe.DocumentosEletronicos.NFe.Wsdl.ConsultaProtocolo;
using DFe.DocumentosEletronicos.NFe.Wsdl.DistribuicaoDFe;
using DFe.DocumentosEletronicos.NFe.Wsdl.Download;
using DFe.DocumentosEletronicos.NFe.Wsdl.Evento;
using DFe.DocumentosEletronicos.NFe.Wsdl.Inutilizacao;
using DFe.DocumentosEletronicos.NFe.Wsdl.Recepcao;
using DFe.DocumentosEletronicos.NFe.Wsdl.Status;
using DFe.Ext;
using DFe.Utils.Assinatura;
using detEvento = DFe.DocumentosEletronicos.NFe.Classes.Servicos.Evento.detEvento;
using evento = DFe.DocumentosEletronicos.NFe.Classes.Servicos.Evento.evento;
using FuncoesXml = DFe.DocumentosEletronicos.ManipuladorDeXml.FuncoesXml;
using procEventoNFe = DFe.DocumentosEletronicos.NFe.Classes.Retorno.Consulta.procEventoNFe;

namespace DFe.DocumentosEletronicos.NFe.Servicos
{
    public sealed class ServicosNFe : IDisposable
    {
        private readonly X509Certificate2 _certificado;
        private readonly ConfiguracaoServico _cFgServico;
        private readonly string _path;

        /// <summary>
        ///     Cria uma instância da Classe responsável pelos serviços relacionados à NFe
        /// </summary>
        /// <param name="cFgServico"></param>
        public ServicosNFe(ConfiguracaoServico cFgServico)
        {
            _cFgServico = cFgServico;
            _certificado = CertificadoDigital.ObterCertificado(cFgServico.Certificado);
            _path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            //Define a versão do protocolo de segurança
            ServicePointManager.SecurityProtocol = cFgServico.ProtocoloDeSeguranca;
        }

        private void SalvarArquivoXml(string nomeArquivo, string xmlString)
        {
            if (!_cFgServico.SalvarXmlServicos) return;
            var dir = string.IsNullOrEmpty(_cFgServico.DiretorioSalvarXml) ? _path : _cFgServico.DiretorioSalvarXml;
            var stw = new StreamWriter(dir + @"\" + nomeArquivo);
            stw.WriteLine(xmlString);
            stw.Close();
        }

        private INfeServicoAutorizacao CriarServicoAutorizacao(ServicoNFe servico)
        {
            var url = string.Empty; // todo Enderecador.ObterUrlServico(servico, _cFgServico);
            if (servico != ServicoNFe.NFeAutorizacao)
                throw new Exception(
                    string.Format("O serviço {0} não pode ser criado no método {1}!", servico,
                        MethodBase.GetCurrentMethod().Name));
            if (_cFgServico.cUF == Estado.PR & _cFgServico.VersaoNFeAutorizacao == VersaoServico.Versao310)
                return new NfeAutorizacao3(url, _certificado, _cFgServico.TimeOut);
            return new NfeAutorizacao(url, _certificado, _cFgServico.TimeOut);
        }

        private INfeServico CriarServico(ServicoNFe servico)
        {
            var url = string.Empty; // todo Enderecador.ObterUrlServico(servico, _cFgServico);
            switch (servico)
            {
                case ServicoNFe.NfeStatusServico:
                    if (_cFgServico.VersaoNfeStatusServico == VersaoServico.Versao400)
                    {
                        return new NFeStatusServico4(url, _certificado, _cFgServico.TimeOut);
                    }
                    if (_cFgServico.cUF == Estado.PR & _cFgServico.VersaoNfeStatusServico == VersaoServico.Versao310)
                    {
                        return new NfeStatusServico3(url, _certificado, _cFgServico.TimeOut);
                    }
                    if (_cFgServico.cUF == Estado.BA & _cFgServico.VersaoNfeStatusServico == VersaoServico.Versao310 &
                        _cFgServico.ModeloDocumento == ModeloDocumento.NFe)
                    {
                        return new NfeStatusServico(url, _certificado, _cFgServico.TimeOut);
                    }
                    return new NfeStatusServico2(url, _certificado, _cFgServico.TimeOut);

                case ServicoNFe.NfeConsultaProtocolo:
                    if (_cFgServico.cUF == Estado.PR & _cFgServico.VersaoNfeConsultaProtocolo == VersaoServico.Versao310)
                    {
                        return new NfeConsulta3(url, _certificado, _cFgServico.TimeOut);
                    }
                    if (_cFgServico.cUF == Estado.BA & _cFgServico.VersaoNfeConsultaProtocolo == VersaoServico.Versao310 &
                        _cFgServico.ModeloDocumento == ModeloDocumento.NFe)
                    {
                        return new NfeConsulta(url, _certificado, _cFgServico.TimeOut);
                    }
                    return new NfeConsulta2(url, _certificado, _cFgServico.TimeOut);

                case ServicoNFe.NfeRecepcao:
                    return new NfeRecepcao2(url, _certificado, _cFgServico.TimeOut);

                case ServicoNFe.NfeRetRecepcao:
                    return new NfeRetRecepcao2(url, _certificado, _cFgServico.TimeOut);

                case ServicoNFe.NFeAutorizacao:
                    throw new Exception(string.Format("O serviço {0} não pode ser criado no método {1}!", servico,
                        MethodBase.GetCurrentMethod().Name));

                case ServicoNFe.NFeRetAutorizacao:
                    if (_cFgServico.cUF == Estado.PR & _cFgServico.VersaoNFeAutorizacao == VersaoServico.Versao310)
                        return new NfeRetAutorizacao3(url, _certificado, _cFgServico.TimeOut);
                    return new NfeRetAutorizacao(url, _certificado, _cFgServico.TimeOut);

                case ServicoNFe.NfeInutilizacao:
                    if (_cFgServico.cUF == Estado.PR & _cFgServico.VersaoNfeStatusServico == VersaoServico.Versao310)
                    {
                        return new NfeInutilizacao3(url, _certificado, _cFgServico.TimeOut);
                    }
                    if (_cFgServico.cUF == Estado.BA & _cFgServico.VersaoNfeStatusServico == VersaoServico.Versao310 &
                        _cFgServico.ModeloDocumento == ModeloDocumento.NFe)
                    {
                        return new NfeInutilizacao(url, _certificado, _cFgServico.TimeOut);
                    }
                    return new NfeInutilizacao2(url, _certificado, _cFgServico.TimeOut);

                case ServicoNFe.RecepcaoEventoCancelmento:
                case ServicoNFe.RecepcaoEventoCartaCorrecao:
                case ServicoNFe.RecepcaoEventoManifestacaoDestinatario:
                    return new RecepcaoEvento(url, _certificado, _cFgServico.TimeOut);
                case ServicoNFe.RecepcaoEventoEpec:
                    return new RecepcaoEPEC(url, _certificado, _cFgServico.TimeOut);

                case ServicoNFe.NfeConsultaCadastro:
                    switch (_cFgServico.cUF)
                    {
                        case Estado.CE:
                            return new CadConsultaCadastro2(url, _certificado,
                                _cFgServico.TimeOut);
                    }
                    return new Wsdl.ConsultaCadastro.DEMAIS_UFs.CadConsultaCadastro2(url, _certificado,
                        _cFgServico.TimeOut);

                case ServicoNFe.NfeDownloadNF:
                    return new NfeDownloadNF(url, _certificado, _cFgServico.TimeOut);

                case ServicoNFe.NfceAdministracaoCSC:
                    return new NfceCsc(url, _certificado, _cFgServico.TimeOut);

                case ServicoNFe.NFeDistribuicaoDFe:
                    return new NfeDistDFeInteresse(url, _certificado, _cFgServico.TimeOut);

            }

            return null;
        }

        /// <summary>
        ///     Consulta o status do Serviço de NFe 
        /// </summary>
        /// <returns>Retorna um objeto da classe RetornoNfeStatusServico com os dados status do serviço</returns>
        public RetornoNfeStatusServico NfeStatusServico()
        {
            var versaoServico = ServicoNFe.NfeStatusServico.VersaoServicoParaString(_cFgServico.VersaoNfeStatusServico);

            #region Cria o objeto wdsl para consulta
            
            var ws = CriarServico(ServicoNFe.NfeStatusServico);

            ws.nfeCabecMsg = new nfeCabecMsg
            {
                cUF = _cFgServico.cUF,
                versaoDados = versaoServico
            };

            #endregion

            #region Cria o objeto consStatServ

            var pedStatus = new consStatServ
            {
                cUF = _cFgServico.cUF,
                tpAmb = _cFgServico.tpAmb,
                versao = versaoServico
            };

            #endregion

            #region Valida, Envia os dados e obtém a resposta

            var xmlStatus = pedStatus.ObterXmlString();
            Validador.Valida(ServicoNFe.NfeStatusServico, _cFgServico.VersaoNfeStatusServico, xmlStatus);
            var dadosStatus = new XmlDocument();
            dadosStatus.LoadXml(xmlStatus);

            SalvarArquivoXml(DateTime.Now.ParaDataHoraString() + "-ped-sta.xml", xmlStatus);

            XmlNode retorno;
            try
            {
                retorno = ws.Execute(dadosStatus);
            }
            catch (WebException ex)
            {
                throw FabricaComunicacaoException.ObterException(ServicoNFe.NfeStatusServico, ex);
            }

            var retornoXmlString = retorno.OuterXml;
            var retConsStatServ = new retConsStatServ().CarregarDeXmlString(retornoXmlString);

            SalvarArquivoXml(DateTime.Now.ParaDataHoraString() + "-sta.xml", retornoXmlString);

            return new RetornoNfeStatusServico(pedStatus.ObterXmlString(), ExtretConsStatServ.ObterXmlString(retConsStatServ),
                retornoXmlString, retConsStatServ);

            #endregion
        }

        /// <summary>
        ///     Consulta a Situação da NFe
        /// </summary>
        /// <returns>Retorna um objeto da classe RetornoNfeConsultaProtocolo com os dados da Situação da NFe</returns>
        public RetornoNfeConsultaProtocolo NfeConsultaProtocolo(string chave)
        {
            var versaoServico =
                ServicoNFe.NfeConsultaProtocolo.VersaoServicoParaString(_cFgServico.VersaoNfeConsultaProtocolo);

            #region Cria o objeto wdsl para consulta

            var ws = CriarServico(ServicoNFe.NfeConsultaProtocolo);

            ws.nfeCabecMsg = new nfeCabecMsg
            {
                cUF = _cFgServico.cUF,
                versaoDados = versaoServico
            };

            #endregion

            #region Cria o objeto consSitNFe

            var pedConsulta = new consSitNFe
            {
                versao = versaoServico,
                tpAmb = _cFgServico.tpAmb,
                chNFe = chave
            };

            #endregion

            #region Valida, Envia os dados e obtém a resposta

            var xmlConsulta = pedConsulta.ObterXmlString();
            Validador.Valida(ServicoNFe.NfeConsultaProtocolo, _cFgServico.VersaoNfeConsultaProtocolo, xmlConsulta);
            var dadosConsulta = new XmlDocument();
            dadosConsulta.LoadXml(xmlConsulta);

            SalvarArquivoXml(chave + "-ped-sit.xml", xmlConsulta);

            XmlNode retorno;
            try
            {
                retorno = ws.Execute(dadosConsulta);
            }
            catch (WebException ex)
            {
                throw FabricaComunicacaoException.ObterException(ServicoNFe.NfeConsultaProtocolo, ex);
            }

            var retornoXmlString = retorno.OuterXml;
            var retConsulta = new retConsSitNFe().CarregarDeXmlString(retornoXmlString);

            SalvarArquivoXml(chave + "-sit.xml", retornoXmlString);

            return new RetornoNfeConsultaProtocolo(pedConsulta.ObterXmlString(), ExtretConsSitNFe.ObterXmlString(retConsulta),
                retornoXmlString, retConsulta);

            #endregion
        }

        /// <summary>
        ///     Inutiliza uma faixa de números
        /// </summary>
        /// <param name="cnpj"></param>
        /// <param name="ano"></param>
        /// <param name="modelo"></param>
        /// <param name="serie"></param>
        /// <param name="numeroInicial"></param>
        /// <param name="numeroFinal"></param>
        /// <param name="justificativa"></param>
        /// <returns>Retorna um objeto da classe RetornoNfeInutilizacao com o retorno do serviço NfeInutilizacao</returns>
        public RetornoNfeInutilizacao NfeInutilizacao(string cnpj, int ano, ModeloDocumento modelo, int serie,
            int numeroInicial, int numeroFinal, string justificativa)
        {
            var versaoServico = ServicoNFe.NfeInutilizacao.VersaoServicoParaString(_cFgServico.VersaoNfeInutilizacao);

            #region Cria o objeto wdsl para consulta

            var ws = CriarServico(ServicoNFe.NfeInutilizacao);

            ws.nfeCabecMsg = new nfeCabecMsg
            {
                cUF = _cFgServico.cUF,
                versaoDados = versaoServico
            };

            #endregion

            #region Cria o objeto inutNFe

            var pedInutilizacao = new inutNFe
            {
                versao = versaoServico,
                infInut = new infInutEnv
                {
                    tpAmb = _cFgServico.tpAmb,
                    cUF = _cFgServico.cUF,
                    ano = ano,
                    CNPJ = cnpj,
                    mod = modelo,
                    serie = serie,
                    nNFIni = numeroInicial,
                    nNFFin = numeroFinal,
                    xJust = justificativa
                }
            };

            var numId = string.Concat((int) pedInutilizacao.infInut.cUF, pedInutilizacao.infInut.ano,
                pedInutilizacao.infInut.CNPJ, (int) pedInutilizacao.infInut.mod,
                pedInutilizacao.infInut.serie.ToString().PadLeft(3, '0'),
                pedInutilizacao.infInut.nNFIni.ToString().PadLeft(9, '0'),
                pedInutilizacao.infInut.nNFFin.ToString().PadLeft(9, '0'));
            pedInutilizacao.infInut.Id = "ID" + numId;

            pedInutilizacao.Assina(_certificado);

            #endregion

            #region Valida, Envia os dados e obtém a resposta

            var xmlInutilizacao = pedInutilizacao.ObterXmlString();
            Validador.Valida(ServicoNFe.NfeInutilizacao, _cFgServico.VersaoNfeInutilizacao, xmlInutilizacao);
            var dadosInutilizacao = new XmlDocument();
            dadosInutilizacao.LoadXml(xmlInutilizacao);

            SalvarArquivoXml(numId + "-ped-inu.xml", xmlInutilizacao);

            XmlNode retorno;
            try
            {
                retorno = ws.Execute(dadosInutilizacao);
            }
            catch (WebException ex)
            {
                throw FabricaComunicacaoException.ObterException(ServicoNFe.NfeInutilizacao, ex);
            }

            var retornoXmlString = retorno.OuterXml;
            var retInutNFe = new retInutNFe().CarregarDeXmlString(retornoXmlString);

            SalvarArquivoXml(numId + "-inu.xml", retornoXmlString);

            return new RetornoNfeInutilizacao(pedInutilizacao.ObterXmlString(), ExtretInutNFe.ObterXmlString(retInutNFe),
                retornoXmlString, retInutNFe);

            #endregion
        }

        /// <summary>
        ///     Envia um evento genérico
        /// </summary>
        /// <param name="idlote"></param>
        /// <param name="eventos"></param>
        /// <param name="servicoEvento">Tipo de serviço do evento: valores válidos: RecepcaoEventoCancelmento, RecepcaoEventoCartaCorrecao, RecepcaoEventoEpec e RecepcaoEventoManifestacaoDestinatario</param>
        /// <returns>Retorna um objeto da classe RetornoRecepcaoEvento com o retorno do serviço RecepcaoEvento</returns>
        private RetornoRecepcaoEvento RecepcaoEvento(int idlote, List<evento> eventos, ServicoNFe servicoEvento)
        {
            var listaEventos = new List<ServicoNFe>
            {
                ServicoNFe.RecepcaoEventoCartaCorrecao,
                ServicoNFe.RecepcaoEventoCancelmento,
                ServicoNFe.RecepcaoEventoEpec,
                ServicoNFe.RecepcaoEventoManifestacaoDestinatario
            };
            if (
                !listaEventos.Contains(servicoEvento))
                throw new Exception(
                    string.Format("Serviço {0} é inválido para o método {1}!\nServiços válidos: \n • {2}", servicoEvento,
                        MethodBase.GetCurrentMethod().Name, string.Join("\n • ", listaEventos.ToArray())));

            var versaoServico = servicoEvento.VersaoServicoParaString(_cFgServico.VersaoRecepcaoEventoCceCancelamento);

            #region Cria o objeto wdsl para consulta

            var ws = CriarServico(servicoEvento);

            ws.nfeCabecMsg = new nfeCabecMsg
            {
                cUF = _cFgServico.cUF,
                versaoDados = versaoServico
            };

            #endregion

            #region Cria o objeto envEvento

            var pedEvento = new envEvento
            {
                versao = versaoServico,
                idLote = idlote,
                evento = eventos
            };

            foreach (var evento in eventos)
            {
                evento.infEvento.Id = "ID" + evento.infEvento.tpEvento + evento.infEvento.chNFe +
                                      evento.infEvento.nSeqEvento.ToString().PadLeft(2, '0');
                evento.Assina(_certificado);
            }

            #endregion

            #region Valida, Envia os dados e obtém a resposta

            var xmlEvento = pedEvento.ObterXmlString();
            Validador.Valida(servicoEvento, _cFgServico.VersaoRecepcaoEventoCceCancelamento, xmlEvento);
            var dadosEvento = new XmlDocument();
            dadosEvento.LoadXml(xmlEvento);

            SalvarArquivoXml(idlote + "-ped-eve.xml", xmlEvento);

            XmlNode retorno;
            try
            {
                retorno = ws.Execute(dadosEvento);
            }
            catch (WebException ex)
            {
                throw FabricaComunicacaoException.ObterException(servicoEvento, ex);
            }

            var retornoXmlString = retorno.OuterXml;
            var retEnvEvento = new retEnvEvento().CarregarDeXmlString(retornoXmlString);

            SalvarArquivoXml(idlote + "-eve.xml", retornoXmlString);

            #region Obtém um procEventoNFe de cada evento e salva em arquivo

            var listprocEventoNFe = new List<procEventoNFe>();

            foreach (var evento in eventos)
            {
                var eve = evento;
                var retEvento = (from retevento in retEnvEvento.retEvento
                    where
                    retevento.infEvento.chNFe == eve.infEvento.chNFe &&
                    retevento.infEvento.tpEvento == eve.infEvento.tpEvento
                    select retevento).SingleOrDefault();

                var procevento = new procEventoNFe {evento = eve, versao = eve.versao, retEvento = retEvento};
                listprocEventoNFe.Add(procevento);
                if (!_cFgServico.SalvarXmlServicos) continue;
                var proceventoXmlString = procevento.ObterXmlString();
                SalvarArquivoXml(procevento.evento.infEvento.Id.Substring(2) + "-procEventoNFe.xml", proceventoXmlString);
            }

            #endregion

            return new RetornoRecepcaoEvento(pedEvento.ObterXmlString(), ExtretEnvEvento.ObterXmlString(retEnvEvento), retornoXmlString,
                retEnvEvento, listprocEventoNFe);

            #endregion
        }

        /// <summary>
        ///     Envia um evento do tipo "Cancelamento"
        /// </summary>
        /// <param name="idlote"></param>
        /// <param name="sequenciaEvento"></param>
        /// <param name="protocoloAutorizacao"></param>
        /// <param name="chaveNFe"></param>
        /// <param name="justificativa"></param>
        /// <param name="cpfcnpj"></param>
        /// <returns>Retorna um objeto da classe RetornoRecepcaoEvento com o retorno do serviço RecepcaoEvento</returns>
        public RetornoRecepcaoEvento RecepcaoEventoCancelamento(int idlote, int sequenciaEvento,
            string protocoloAutorizacao, string chaveNFe, string justificativa, string cpfcnpj)
        {
            var versaoServico =
                ServicoNFe.RecepcaoEventoCancelmento.VersaoServicoParaString(
                    _cFgServico.VersaoRecepcaoEventoCceCancelamento);
            var detEvento = new detEvento {nProt = protocoloAutorizacao, versao = versaoServico, xJust = justificativa};
            var infEvento = new infEventoEnv
            {
                cOrgao = _cFgServico.cUF,
                tpAmb = _cFgServico.tpAmb,
                chNFe = chaveNFe,
                dhEvento = DateTime.Now,
                tpEvento = 110111,
                nSeqEvento = sequenciaEvento,
                verEvento = versaoServico,
                detEvento = detEvento
            };
            if (cpfcnpj.Length == 11)
                infEvento.CPF = cpfcnpj;
            else
                infEvento.CNPJ = cpfcnpj;

            var evento = new evento {versao = versaoServico, infEvento = infEvento};

            var retorno = RecepcaoEvento(idlote, new List<evento> {evento}, ServicoNFe.RecepcaoEventoCancelmento);
            return retorno;
        }

        /// <summary>
        ///     Envia um evento do tipo "Carta de Correção"
        /// </summary>
        /// <param name="idlote"></param>
        /// <param name="sequenciaEvento"></param>
        /// <param name="chaveNFe"></param>
        /// <param name="correcao"></param>
        /// <param name="cpfcnpj"></param>
        /// <returns>Retorna um objeto da classe RetornoRecepcaoEvento com o retorno do serviço RecepcaoEvento</returns>
        public RetornoRecepcaoEvento RecepcaoEventoCartaCorrecao(int idlote, int sequenciaEvento, string chaveNFe,
            string correcao, string cpfcnpj)
        {
            var versaoServico =
                ServicoNFe.RecepcaoEventoCartaCorrecao.VersaoServicoParaString(
                    _cFgServico.VersaoRecepcaoEventoCceCancelamento);
            var detEvento = new detEvento {versao = versaoServico, xCorrecao = correcao, xJust = null};
            var infEvento = new infEventoEnv
            {
                cOrgao = _cFgServico.cUF,
                tpAmb = _cFgServico.tpAmb,
                chNFe = chaveNFe,
                dhEvento = DateTime.Now,
                tpEvento = 110110,
                nSeqEvento = sequenciaEvento,
                verEvento = versaoServico,
                detEvento = detEvento
            };
            if (cpfcnpj.Length == 11)
                infEvento.CPF = cpfcnpj;
            else
                infEvento.CNPJ = cpfcnpj;

            var evento = new evento {versao = versaoServico, infEvento = infEvento};

            var retorno = RecepcaoEvento(idlote, new List<evento> {evento}, ServicoNFe.RecepcaoEventoCartaCorrecao);
            return retorno;
        }

        public RetornoRecepcaoEvento RecepcaoEventoManifestacaoDestinatario(int idlote, int sequenciaEvento,
                    string chaveNFe, TipoEventoManifestacaoDestinatario tipoEventoManifestacaoDestinatario, string cpfcnpj,
                    string justificativa = null)
        {
            return RecepcaoEventoManifestacaoDestinatario(idlote, sequenciaEvento, new[] { chaveNFe },
                tipoEventoManifestacaoDestinatario, cpfcnpj, justificativa);
        }

        public RetornoRecepcaoEvento RecepcaoEventoManifestacaoDestinatario(int idlote, int sequenciaEvento,
            string[] chavesNFe, TipoEventoManifestacaoDestinatario tipoEventoManifestacaoDestinatario, string cpfcnpj,
            string justificativa = null)
        {
            var versaoServico =
                ServicoNFe.RecepcaoEventoManifestacaoDestinatario.VersaoServicoParaString(
                    _cFgServico.VersaoRecepcaoEventoCceCancelamento);
            var detEvento = new detEvento
            {
                versao = versaoServico,
                descEvento = tipoEventoManifestacaoDestinatario.Descricao(),
                xJust = justificativa
            };

            var eventos = new List<evento>();
            foreach (var chaveNFe in chavesNFe)
            {
                var infEvento = new infEventoEnv
                {
                    cOrgao = _cFgServico.cUF == Estado.RS ? _cFgServico.cUF : Estado.AN,
                    //RS possui endereço próprio para manifestação do destinatário. Demais UFs usam o ambiente nacional
                    tpAmb = _cFgServico.tpAmb,
                    chNFe = chaveNFe,
                    dhEvento = DateTime.Now,
                    tpEvento = (int)tipoEventoManifestacaoDestinatario,
                    nSeqEvento = sequenciaEvento,
                    verEvento = versaoServico,
                    detEvento = detEvento
                };
                if (cpfcnpj.Length == 11)
                    infEvento.CPF = cpfcnpj;
                else
                    infEvento.CNPJ = cpfcnpj;

                eventos.Add(new evento { versao = versaoServico, infEvento = infEvento });
            }


            var retorno = RecepcaoEvento(idlote, eventos,
                ServicoNFe.RecepcaoEventoManifestacaoDestinatario);
            return retorno;
        }

        /// <summary>
        ///     Envia um evento do tipo "EPEC"
        /// </summary>
        /// <param name="idlote"></param>
        /// <param name="sequenciaEvento"></param>
        /// <param name="nfe"></param>
        /// <param name="veraplic"></param>
        /// <returns>Retorna um objeto da classe RetornoRecepcaoEvento com o retorno do serviço RecepcaoEvento</returns>
        public RetornoRecepcaoEvento RecepcaoEventoEpec(int idlote, int sequenciaEvento, Classes.Informacoes.NFe nfe,
            string veraplic)
        {
            var versaoServico =
                ServicoNFe.RecepcaoEventoEpec.VersaoServicoParaString(_cFgServico.VersaoRecepcaoEventoCceCancelamento);

            if (string.IsNullOrEmpty(nfe.infNFe.Id))
                ExtNFe.Valida(nfe.Assina());

            var detevento = new detEvento
            {
                versao = versaoServico,
                cOrgaoAutor = nfe.infNFe.ide.cUF,
                tpAutor = TipoAutor.taEmpresaEmitente,
                verAplic = veraplic,
                dhEmi = nfe.infNFe.ide.dhEmi,
                tpNF = nfe.infNFe.ide.tpNF,
                IE = nfe.infNFe.emit.IE,
                dest = new dest
                {
                    UF = nfe.infNFe.dest.enderDest.UF,
                    CNPJ = nfe.infNFe.dest.CNPJ,
                    CPF = nfe.infNFe.dest.CPF,
                    IE = nfe.infNFe.dest.IE,
                    vNF = nfe.infNFe.total.ICMSTot.vNF,
                    vICMS = nfe.infNFe.total.ICMSTot.vICMS,
                    vST = nfe.infNFe.total.ICMSTot.vST
                }
            };

            var infEvento = new infEventoEnv
            {
                cOrgao = Estado.AN,
                tpAmb = nfe.infNFe.ide.tpAmb,
                CNPJ = nfe.infNFe.emit.CNPJ,
                CPF = nfe.infNFe.emit.CPF,
                chNFe = nfe.infNFe.Id.Substring(3),
                dhEvento = DateTime.Now,
                tpEvento = 110140,
                nSeqEvento = sequenciaEvento,
                verEvento = versaoServico,
                detEvento = detevento
            };

            var evento = new evento {versao = versaoServico, infEvento = infEvento};

            var retorno = RecepcaoEvento(idlote, new List<evento> {evento}, ServicoNFe.RecepcaoEventoEpec);
            return retorno;
        }

        /// <summary>
        ///     Consulta a situação cadastral, com base na UF/Documento
        ///     <para>O documento pode ser: IE, CNPJ ou CPF</para>
        /// </summary>
        /// <param name="uf">Sigla da UF consultada, informar 'SU' para SUFRAMA</param>
        /// <param name="tipoDocumento">Tipo de documento a ser consultado</param>
        /// <param name="documento">Documento a ser consultado</param>
        /// <returns>Retorna um objeto da classe RetornoNfeConsultaCadastro com o retorno do serviço NfeConsultaCadastro</returns>
        public RetornoNfeConsultaCadastro NfeConsultaCadastro(string uf, ConsultaCadastroTipoDocumento tipoDocumento,
            string documento)
        {
            var versaoServico =
                ServicoNFe.NfeConsultaCadastro.VersaoServicoParaString(_cFgServico.VersaoNfeConsultaCadastro);

            #region Cria o objeto wdsl para consulta

            var ws = CriarServico(ServicoNFe.NfeConsultaCadastro);

            ws.nfeCabecMsg = new nfeCabecMsg
            {
                cUF = _cFgServico.cUF,
                versaoDados = versaoServico
            };

            #endregion

            #region Cria o objeto ConsCad

            var pedConsulta = new ConsCad
            {
                versao = versaoServico,
                infCons = new infConsEnv {UF = uf}
            };

            switch (tipoDocumento)
            {
                case ConsultaCadastroTipoDocumento.Ie:
                    pedConsulta.infCons.IE = documento;
                    break;
                case ConsultaCadastroTipoDocumento.Cnpj:
                    pedConsulta.infCons.CNPJ = documento;
                    break;
                case ConsultaCadastroTipoDocumento.Cpf:
                    pedConsulta.infCons.CPF = documento;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("tipoDocumento", tipoDocumento, null);
            }

            #endregion

            #region Valida, Envia os dados e obtém a resposta

            var xmlConsulta = pedConsulta.ObterXmlString();
            Validador.Valida(ServicoNFe.NfeConsultaCadastro, _cFgServico.VersaoNfeConsultaCadastro, xmlConsulta);
            var dadosConsulta = new XmlDocument();
            dadosConsulta.LoadXml(xmlConsulta);

            SalvarArquivoXml(DateTime.Now.ParaDataHoraString() + "-ped-cad.xml", xmlConsulta);

            XmlNode retorno;
            try
            {
                retorno = ws.Execute(dadosConsulta);
            }
            catch (WebException ex)
            {
                throw FabricaComunicacaoException.ObterException(ServicoNFe.NfeConsultaCadastro, ex);
            }

            var retornoXmlString = retorno.OuterXml;
            var retConsulta = new retConsCad().CarregarDeXmlString(retornoXmlString);

            SalvarArquivoXml(DateTime.Now.ParaDataHoraString() + "-cad.xml", retornoXmlString);

            return new RetornoNfeConsultaCadastro(pedConsulta.ObterXmlString(), ExtretConsCad.ObterXmlString(retConsulta),
                retornoXmlString, retConsulta);

            #endregion
        }

        /// <summary>
        /// Serviço destinado à distribuição de informações resumidas e documentos fiscais eletrônicos de interesse de um ator, seja este pessoa física ou jurídica.
        /// </summary>
        /// <param name="ufAutor">Código da UF do Autor</param>
        /// <param name="documento">CNPJ/CPF do interessado no DF-e</param>
        /// <param name="ultNSU">Último NSU recebido pelo Interessado</param>
        /// <param name="nSU">Número Sequencial Único</param>
        /// <returns>Retorna um objeto da classe RetornoNfeDistDFeInt com os documentos de interesse do CNPJ/CPF pesquisado</returns>
        public RetornoNfeDistDFeInt NfeDistDFeInteresse(string ufAutor, string documento, string ultNSU = "0", string nSU = "0", string chNFE = "")
        {
            var versaoServico = ServicoNFe.NFeDistribuicaoDFe.VersaoServicoParaString(_cFgServico.VersaoNFeDistribuicaoDFe);

            #region Cria o objeto wdsl para consulta

            var ws = CriarServico(ServicoNFe.NFeDistribuicaoDFe);
            ws.nfeCabecMsg = new nfeCabecMsg
            {
                cUF = _cFgServico.cUF,
                versaoDados = versaoServico
            };

            #endregion

            #region Cria o objeto distDFeInt

            var pedDistDFeInt = new distDFeInt
            {
                versao = versaoServico,
                tpAmb = _cFgServico.tpAmb,
                cUFAutor = _cFgServico.cUF
            };

            if (documento.Length == 11)
                pedDistDFeInt.CPF = documento;
            if (documento.Length > 11)
                pedDistDFeInt.CNPJ = documento;

            if (string.IsNullOrEmpty(chNFE))
                pedDistDFeInt.distNSU = new distNSU { ultNSU = ultNSU.PadLeft(15, '0') };

            if (!nSU.Equals("0"))
            {
                pedDistDFeInt.consNSU = new consNSU { NSU = nSU.PadLeft(15, '0') };
                pedDistDFeInt.distNSU = null;
                pedDistDFeInt.consChNFe = null;
            }

            if (!string.IsNullOrEmpty(chNFE))
            {
                pedDistDFeInt.consChNFe = new consChNFe { chNFe = chNFE };
                pedDistDFeInt.consNSU = null;
                pedDistDFeInt.distNSU = null;
            }

            #endregion

            #region Valida, Envia os dados e obtém a resposta

            var xmlConsulta = pedDistDFeInt.ObterXmlString();
            Validador.Valida(ServicoNFe.NFeDistribuicaoDFe, _cFgServico.VersaoNFeDistribuicaoDFe, xmlConsulta);
            var dadosConsulta = new XmlDocument();
            dadosConsulta.LoadXml(xmlConsulta);

            SalvarArquivoXml(DateTime.Now.ParaDataHoraString() + "-ped-DistDFeInt.xml", xmlConsulta);

            XmlNode retorno;
            try
            {
                retorno = ws.Execute(dadosConsulta);
            }
            catch (WebException ex)
            {
                throw FabricaComunicacaoException.ObterException(ServicoNFe.NFeDistribuicaoDFe, ex);
            }

            var retornoXmlString = retorno.OuterXml;
            var retConsulta = new retDistDFeInt().CarregarDeXmlString(retornoXmlString);

            SalvarArquivoXml(DateTime.Now.ParaDataHoraString() + "-distDFeInt.xml", retornoXmlString);

            #region Obtém um retDistDFeInt de cada evento e salva em arquivo

            if (retConsulta.loteDistDFeInt != null)
            {
                for (int i = 0; i < retConsulta.loteDistDFeInt.Length; i++)
                {
                    string conteudo = Compressao.Unzip(retConsulta.loteDistDFeInt[i].XmlNfe);
                    string chNFe = string.Empty;

                    if (conteudo.StartsWith("<resNFe"))
                    {
                        var retConteudo =
                            FuncoesXml.XmlStringParaClasse<Classes.Servicos.DistribuicaoDFe.Schemas.resNFe>(conteudo);
                        chNFe = retConteudo.chNFe;
                    }
                    else if (conteudo.StartsWith("<procEventoNFe"))
                    {
                        var procEventoNFeConteudo =
                            FuncoesXml.XmlStringParaClasse<Classes.Servicos.DistribuicaoDFe.Schemas.procEventoNFe>(conteudo);
                        chNFe = procEventoNFeConteudo.retEvento.infEvento.chNFe;
                    }
                    else if (conteudo.StartsWith("<resEvento"))
                    {
                        var resEventoConteudo =
                            FuncoesXml.XmlStringParaClasse<Classes.Servicos.DistribuicaoDFe.Schemas.resEvento>(conteudo);
                        chNFe = resEventoConteudo.chNFe;
                    }

                    string[] schema = retConsulta.loteDistDFeInt[i].schema.Split('_');
                    if (chNFe == string.Empty)
                        chNFe = DateTime.Now.ParaDataHoraString() + "_SEMCHAVE";

                    SalvarArquivoXml(chNFe + "-" + schema[0] + ".xml", conteudo);
                }
            }

            #endregion

            return new RetornoNfeDistDFeInt(pedDistDFeInt.ObterXmlString(), retConsulta.ObterXmlString(), retornoXmlString, retConsulta);

            #endregion

        }

        #region Recepção

        /// <summary>
        ///     Envia uma ou mais NFe
        /// </summary>
        /// <param name="idLote"></param>
        /// <param name="nFes"></param>
        /// <returns>Retorna um objeto da classe RetornoNfeRecepcao com com os dados do resultado da transmissão</returns>
        public RetornoNfeRecepcao NfeRecepcao(int idLote, List<Classes.Informacoes.NFe> nFes)
        {
            var versaoServico = ServicoNFe.NfeRecepcao.VersaoServicoParaString(_cFgServico.VersaoNfeRecepcao);

            #region Cria o objeto wdsl para consulta

            var ws = CriarServico(ServicoNFe.NfeRecepcao);

            ws.nfeCabecMsg = new nfeCabecMsg
            {
                cUF = _cFgServico.cUF,
                versaoDados = versaoServico
            };

            #endregion

            #region Cria o objeto enviNFe

            var pedEnvio = new enviNFe2(versaoServico, idLote, nFes);

            #endregion

            #region Valida, Envia os dados e obtém a resposta

            var xmlEnvio = pedEnvio.ObterXmlString();

            if (_cFgServico.cUF == Estado.PR)
                //Caso o lote seja enviado para o PR, colocar o namespace nos elementos <NFe> do lote, pois o serviço do PR o exige, conforme https://github.com/adeniltonbs/Zeus.Net.NFe.NFCe/issues/33
                xmlEnvio = xmlEnvio.Replace("<NFe>", "<NFe xmlns=\"http://www.portalfiscal.inf.br/nfe\">");

            Validador.Valida(ServicoNFe.NfeRecepcao, _cFgServico.VersaoNfeRecepcao, xmlEnvio);
            var dadosEnvio = new XmlDocument();
            dadosEnvio.LoadXml(xmlEnvio);

            SalvarArquivoXml(idLote + "-env-lot.xml", xmlEnvio);

            XmlNode retorno;
            try
            {
                retorno = ws.Execute(dadosEnvio);
            }
            catch (WebException ex)
            {
                throw FabricaComunicacaoException.ObterException(ServicoNFe.NfeRecepcao, ex);
            }

            var retornoXmlString = retorno.OuterXml;
            var retEnvio = new retEnviNFe().CarregarDeXmlString(retornoXmlString);

            SalvarArquivoXml(idLote + "-rec.xml", retornoXmlString);

            return new RetornoNfeRecepcao(pedEnvio.ObterXmlString(), ExtretEnviNFe.ObterXmlString(retEnvio), retornoXmlString,
                retEnvio);

            #endregion
        }

        /// <summary>
        ///     Recebe o retorno do processamento de uma ou mais NFe's pela SEFAZ
        /// </summary>
        /// <param name="recibo"></param>
        /// <returns>Retorna um objeto da classe RetornoNfeRetRecepcao com com os dados do processamento do lote</returns>
        public RetornoNfeRetRecepcao NfeRetRecepcao(string recibo)
        {
            var versaoServico = ServicoNFe.NfeRetRecepcao.VersaoServicoParaString(_cFgServico.VersaoNfeRetRecepcao);

            #region Cria o objeto wdsl para consulta

            var ws = CriarServico(ServicoNFe.NfeRetRecepcao);

            ws.nfeCabecMsg = new nfeCabecMsg
            {
                cUF = _cFgServico.cUF,
                versaoDados = versaoServico
            };

            #endregion

            #region Cria o objeto consReciNFe

            var pedRecibo = new consReciNFe
            {
                versao = versaoServico,
                tpAmb = _cFgServico.tpAmb,
                nRec = recibo
            };

            #endregion

            #region Envia os dados e obtém a resposta

            var xmlRecibo = pedRecibo.ObterXmlString();
            var dadosRecibo = new XmlDocument();
            dadosRecibo.LoadXml(xmlRecibo);

            SalvarArquivoXml(recibo + "-ped-rec.xml", xmlRecibo);

            XmlNode retorno;
            try
            {
                retorno = ws.Execute(dadosRecibo);
            }
            catch (WebException ex)
            {
                throw FabricaComunicacaoException.ObterException(ServicoNFe.NfeRetRecepcao, ex);
            }

            var retornoXmlString = retorno.OuterXml;
            var retRecibo = new retConsReciNFe().CarregarDeXmlString(retornoXmlString);

            SalvarArquivoXml(recibo + "-pro-rec.xml", retornoXmlString);

            return new RetornoNfeRetRecepcao(pedRecibo.ObterXmlString(), ExtretConsReciNFe.ObterXmlString(retRecibo), retornoXmlString,
                retRecibo);

            #endregion
        }

        #endregion

        #region Autorização

        /// <summary>
        ///     Envia uma ou mais NFe
        /// </summary>
        /// <param name="idLote">ID do Lote</param>
        /// <param name="indSinc">Indicador de Sincronização</param>
        /// <param name="nFes">Lista de NFes a serem enviadas</param>
        /// <param name="compactarMensagem">Define se a mensagem será enviada para a SEFAZ compactada</param>
        /// <returns>Retorna um objeto da classe RetornoNFeAutorizacao com com os dados do resultado da transmissão</returns>
        public RetornoNFeAutorizacao NFeAutorizacao(int idLote, IndicadorSincronizacao indSinc, List<Classes.Informacoes.NFe> nFes,
            bool compactarMensagem = false)
        {
            var versaoServico = ServicoNFe.NFeAutorizacao.VersaoServicoParaString(_cFgServico.VersaoNFeAutorizacao);

            #region Cria o objeto wdsl para consulta

            var ws = CriarServicoAutorizacao(ServicoNFe.NFeAutorizacao);

            ws.nfeCabecMsg = new nfeCabecMsg
            {
                cUF = _cFgServico.cUF,
                versaoDados = versaoServico
            };

            #endregion

            #region Cria o objeto enviNFe

            var pedEnvio = new enviNFe3(versaoServico, idLote, indSinc, nFes);

            #endregion

            #region Valida, Envia os dados e obtém a resposta

            var xmlEnvio = pedEnvio.ObterXmlString();
            if (_cFgServico.cUF == Estado.PR)
                //Caso o lote seja enviado para o PR, colocar o namespace nos elementos <NFe> do lote, pois o serviço do PR o exige, conforme https://github.com/adeniltonbs/Zeus.Net.NFe.NFCe/issues/33
                xmlEnvio = xmlEnvio.Replace("<NFe>", "<NFe xmlns=\"http://www.portalfiscal.inf.br/nfe\">");

            Validador.Valida(ServicoNFe.NFeAutorizacao, _cFgServico.VersaoNFeAutorizacao, xmlEnvio);
            var dadosEnvio = new XmlDocument();
            dadosEnvio.LoadXml(xmlEnvio);

            SalvarArquivoXml(idLote + "-env-lot.xml", xmlEnvio);

            XmlNode retorno;
            try
            {
                if (compactarMensagem)
                {
                    var xmlCompactado = Convert.ToBase64String(Compressao.Zip(xmlEnvio));
                    retorno = ws.ExecuteZip(xmlCompactado);
                }
                else
                {
                    retorno = ws.Execute(dadosEnvio);
                }
            }
            catch (WebException ex)
            {
                throw FabricaComunicacaoException.ObterException(ServicoNFe.NFeAutorizacao, ex);
            }

            var retornoXmlString = retorno.OuterXml;
            var retEnvio = new retEnviNFe().CarregarDeXmlString(retornoXmlString);

            SalvarArquivoXml(idLote + "-rec.xml", retornoXmlString);

            return new RetornoNFeAutorizacao(pedEnvio.ObterXmlString(), ExtretEnviNFe.ObterXmlString(retEnvio), retornoXmlString, retEnvio);

            #endregion
        }

        /// <summary>
        ///     Recebe o retorno do processamento de uma ou mais NFe's pela SEFAZ
        /// </summary>
        /// <param name="recibo"></param>
        /// <returns>Retorna um objeto da classe RetornoNFeRetAutorizacao com com os dados do processamento do lote</returns>
        public RetornoNFeRetAutorizacao NFeRetAutorizacao(string recibo)
        {
            var versaoServico = ServicoNFe.NFeRetAutorizacao.VersaoServicoParaString(_cFgServico.VersaoNFeRetAutorizacao);

            #region Cria o objeto wdsl para consulta

            var ws = CriarServico(ServicoNFe.NFeRetAutorizacao);

            ws.nfeCabecMsg = new nfeCabecMsg
            {
                cUF = _cFgServico.cUF,
                versaoDados = versaoServico
            };

            #endregion

            #region Cria o objeto consReciNFe

            var pedRecibo = new consReciNFe
            {
                versao = versaoServico,
                tpAmb = _cFgServico.tpAmb,
                nRec = recibo
            };

            #endregion

            #region Envia os dados e obtém a resposta

            var xmlRecibo = pedRecibo.ObterXmlString();
            var dadosRecibo = new XmlDocument();
            dadosRecibo.LoadXml(xmlRecibo);

            SalvarArquivoXml(recibo + "-ped-rec.xml", xmlRecibo);

            XmlNode retorno;
            try
            {
                retorno = ws.Execute(dadosRecibo);
            }
            catch (WebException ex)
            {
                throw FabricaComunicacaoException.ObterException(ServicoNFe.NFeRetAutorizacao, ex);
            }

            var retornoXmlString = retorno.OuterXml;
            var retRecibo = new retConsReciNFe().CarregarDeXmlString(retornoXmlString);

            SalvarArquivoXml(recibo + "-pro-rec.xml", retornoXmlString);

            return new RetornoNFeRetAutorizacao(pedRecibo.ObterXmlString(), ExtretConsReciNFe.ObterXmlString(retRecibo), retornoXmlString, retRecibo);

            #endregion
        }

        #endregion

        /// <summary>
        ///     Consulta a Situação da NFe
        /// </summary>
        /// <returns>Retorna um objeto da classe RetornoNfeConsultaProtocolo com os dados da Situação da NFe</returns>
        public RetornoNfeDownload NfeDownloadNf(string cnpj, List<string> chaves, string nomeSaida = "")
        {
            var versaoServico = ServicoNFe.NfeDownloadNF.VersaoServicoParaString(_cFgServico.VersaoNfeDownloadNF);

            #region Cria o objeto wdsl para envio do pedido de Download

            var ws = CriarServico(ServicoNFe.NfeDownloadNF);

            ws.nfeCabecMsg = new nfeCabecMsg
            {
                cUF = _cFgServico.cUF,
                //Embora em http://www.nfe.fazenda.gov.br/portal/webServices.aspx?tipoConteudo=Wak0FwB7dKs=#GO esse serviço está nas versões 2.00 e 3.10, ele rejeita se mandar a versão diferente de 1.00. Testado no Ambiente Nacional - (AN)
                versaoDados = /*versaoServico*/ "1.00" 
            };

            #endregion

            #region Cria o objeto downloadNFe

            var pedDownload = new downloadNFe
            {
                //Embora em http://www.nfe.fazenda.gov.br/portal/webServices.aspx?tipoConteudo=Wak0FwB7dKs=#GO esse serviço está nas versões 2.00 e 3.10, ele rejeita se mandar a versão diferente de 1.00. Testado no Ambiente Nacional - (AN)
                versao = /*versaoServico*/ "1.00",
                CNPJ = cnpj,
                tpAmb = _cFgServico.tpAmb,
                chNFe = chaves
            };

            #endregion

            #region Valida, Envia os dados e obtém a resposta

            var xmlDownload = pedDownload.ObterXmlString();
            Validador.Valida(ServicoNFe.NfeDownloadNF, _cFgServico.VersaoNfeDownloadNF, xmlDownload);
            var dadosDownload = new XmlDocument();
            dadosDownload.LoadXml(xmlDownload);

            if (nomeSaida == "")
            {
                nomeSaida = cnpj;
            }

            SalvarArquivoXml(nomeSaida + "-ped-down.xml", xmlDownload);

            XmlNode retorno;
            try
            {
                retorno = ws.Execute(dadosDownload);
            }
            catch (WebException ex)
            {
                throw FabricaComunicacaoException.ObterException(ServicoNFe.NfeDownloadNF, ex);
            }

            var retornoXmlString = retorno.OuterXml;
            var retDownload = new retDownloadNFe().CarregarDeXmlString(retornoXmlString);

            SalvarArquivoXml(nomeSaida + "-down.xml", retornoXmlString);

            return new RetornoNfeDownload(pedDownload.ObterXmlString(), ExtretDownloadNFe.ObterXmlString(retDownload), retornoXmlString, retDownload);

            #endregion
        }

        #region Adm CSC

        public RetornoAdmCscNFCe AdmCscNFCe(string raizCnpj, IdentificadorOperacaoCsc identificadorOperacaoCsc, string idCscASerRevogado = null, string codigoCscASerRevogado = null)
        {
            var versaoServico = ServicoNFe.NfceAdministracaoCSC.VersaoServicoParaString(_cFgServico.VersaoNfceAministracaoCSC);

            #region Cria o objeto wdsl para envio do pedido de Download

            var ws = CriarServico(ServicoNFe.NfceAdministracaoCSC);

            ws.nfeCabecMsg = new nfeCabecMsg
            {
                cUF = _cFgServico.cUF,
                versaoDados = versaoServico
            };

            #endregion

            #region Cria o objeto downloadNFe

            var admCscNFCe = new admCscNFCe
            {
                versao = versaoServico,
                tpAmb = _cFgServico.tpAmb,
                indOp = identificadorOperacaoCsc,
                raizCNPJ = raizCnpj
            };

            if (identificadorOperacaoCsc == IdentificadorOperacaoCsc.ioRevogaCscAtivo)
            {
                admCscNFCe.dadosCsc = new dadosCsc
                {
                    codigoCsc = codigoCscASerRevogado,
                    idCsc = idCscASerRevogado
                };
            }

            #endregion

            #region Valida, Envia os dados e obtém a resposta

            var xmlAdmCscNfe = admCscNFCe.ObterXmlString();
            var dadosAdmnistracaoCsc = new XmlDocument();
            dadosAdmnistracaoCsc.LoadXml(xmlAdmCscNfe);

            SalvarArquivoXml(raizCnpj + "-adm-csc.xml", xmlAdmCscNfe);

            XmlNode retorno;
            try
            {
                retorno = ws.Execute(dadosAdmnistracaoCsc);
            }
            catch (WebException ex)
            {
                throw FabricaComunicacaoException.ObterException(ServicoNFe.NfceAdministracaoCSC, ex);
            }

            var retornoXmlString = retorno.OuterXml;
            var retCsc = new retAdmCscNFCe().CarregarDeXmlString(retornoXmlString);

            SalvarArquivoXml(raizCnpj + "-ret-adm-csc.xml", retornoXmlString);

            return new RetornoAdmCscNFCe(admCscNFCe.ObterXmlString(), ExtretAdmCscNFCe.ObterXmlString(retCsc), retornoXmlString, retCsc);

            #endregion
        }

        #endregion


        #region Implementação do padrão Dispose

        // Flag: Dispose já foi chamado?
        private bool _disposed;
        
        // Implementação protegida do padrão Dispose.
        private void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
                if (!_cFgServico.Certificado.ManterDadosEmCache)
                    _certificado.Reset();
            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~ServicosNFe()
        {
            Dispose(false);
        }

        #endregion
    }
}