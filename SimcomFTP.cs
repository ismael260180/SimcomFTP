#define PC
using System;

#if G120
using Microsoft.SPOT;
#elif PC
using System.Diagnostics;
#endif

using System.IO;

public enum TipoFTP
{
    LECTURA,
    ESCRITURA,
}
enum Estados
{
    INICIO,
    INACTIVO,
    FTP_CID,
    FTP_SERVIDOR,
    FTP_USUARIO,
    FTP_CLAVE,
    FTP_ARCHIVO,
    FTP_DIRECTORIO,
    ABRIR_ESC_FTP,
    ESCRIBIR_FTP,
    FTP_LEE_ARCHIVO,
    FTP_LEE_DIRECTORIO,
    ABRIR_LEE_FTP,
    LEER_FTP,
    PAUSA_RESPUESTA,
    FTP_FINALIZADO,
    FTP_INI_ESCRIBE,
    FTP_IDLE,
    FTP_IDLE_LECTURA,
    FTP_IDLE_ESCRITURA,
    FTP_GPRS_QUERY,
    FTP_GPRS_ABRIR,
    FTP_GPRS_CONFG,
    FTP_ANA_LECT,
    FTP_ANA_ESC,
    FTP_ESCRIBE_DATOS,
    FTP_DATOS_BUFFER,
    ERROR,
}

enum TipoProceso
{
    LECTURA,
    ESCRITURA,
    NINGUNO,
}



class SimcomFTP
{

    internal enum mensajeError
    {
        NINGUNO = 0,
        NET_ERROR = 61,
        DNS_ERROR = 62,
        CONNECT_TIMEOUT = 63,
        TIMEOUT = 64,
        SERVER_ERROR = 65,
        OPERATION_NOT_ALLOW = 66,
        REPLAY_ERROR = 70,
        USER_ERROR = 71,
        PASSWORD_ERROR = 72,
        TYPE_ERROR = 73,
        REST_ERROR = 74,
        PASSIVE_ERROR = 75,
        ACTIVE_ERROR = 76,
        OPERATE_ERROR = 77,
        UPLOAD_ERROR = 78,
        DOWNLOAD_ERROR = 79,

    }

    internal enum procesoEstado
    {
        FTP_CID,
        FTP_SERVIDOR,
        FTP_USUARIO,
        FTP_CLAVE,
        FTP_ARCHIVO,
        FTP_DIRECTORIO,
        FTP_ESC_LEE,
        FTP_GPRS_QUERY,
        FTP_GPRS_ABRIR,
        FTP_GPRS_CONFG,
        FTP_ESCRIBE_DATOS,
        FTP_FINALIZADO,
    }

    internal enum tipoAlmacenamiento
    {
        RAM,
        SD,
    };
    static public string sServidor;
    static public string sUsuario;
    static public string sClave;
    static public string sDirectorio;
    static public string sArchivo;
    static public string sArchivoSD;
    static public string sDirectorioSD;

    static private Estados eEstadoFTP;

    static internal TipoProceso eTipoProceso;
    static private TipoFTP eTipoTrans;
    static internal tipoAlmacenamiento eTipoAlmace;
    static internal mensajeError eErrorFTP;
    static internal byte[] baBuffer = new byte[10000];
    static int i32CantDatos;
    static int i32IndiceBuffer;
    static private byte[] baBufferRec = new byte[1500];
    static private byte[] baBufferTra = new byte[1024];
    static int i32IndiceBufferTx;


    static internal procesoEstado eProcesoEstado;
    //static int i32ContEspera;
    static int i32IndiceBufferRec = 0;
    static int i32LargoBufferRec = 0;
    // static int i32ContEscLec = 0;
    internal static int i32FTPCodError = -1;
    static int i32FTPCodigo = -1;

    static FileStream fsArchivo = null;

    static stTimer Timer1;
    static stTimer Timer2;

    static public void vfnMestados_SimcomFTP()
    {

        byte[] balBufferTemp;
        int i32lTemp;
        int i32lIndiceTemp = 0;
        byte blTemp;
        int i32lCantDatos = 0;
        switch (eEstadoFTP)
        {
            case Estados.INICIO:

                Timer1 = new stTimer();
                Timer2 = new stTimer();
                eEstadoFTP = Estados.INACTIVO;
                break;

            case Estados.INACTIVO:

                if (stSimcomFTP.boRFlagProcesoOcupado == true)
                {

                    eProcesoEstado = procesoEstado.FTP_GPRS_QUERY;
                    balBufferTemp = FuncionesDatos.baConvertir_bytes("AT+SAPBR=2,1\r\n");
                    //i32ContEspera = 30;
                    Timer1.vfnSetear_Timer(300);
                    UartModem.vfnLimpiar_Puerto();
                    UartModem.vfnEscribir_Puerto(balBufferTemp, balBufferTemp.Length);
                    eEstadoFTP = Estados.PAUSA_RESPUESTA;
                    if (eTipoProceso == TipoProceso.LECTURA)
                        eTipoTrans = TipoFTP.LECTURA;
                    else if (eTipoProceso == TipoProceso.ESCRITURA)
                        eTipoTrans = TipoFTP.ESCRITURA;
                    eErrorFTP = mensajeError.NINGUNO;
                    i32FTPCodError = -1;
                }
                break;

            case Estados.FTP_GPRS_QUERY:

                if ((i32lTemp = UartModem.CantidadBytes) > 5)
                {

                    balBufferTemp = new byte[i32lTemp];
                    UartModem.i32Leer_Puerto(balBufferTemp, i32lTemp);

                    //string mensaje = new string(UTF8Encoding.UTF8.GetChars(balBufferTemp));
                    i32lIndiceTemp = FuncionesDatos.i32Encontrar_InidiceFinal(balBufferTemp, "+SAPBR:", 0);
                    if (i32lIndiceTemp >= 0)
                    {
                        i32lIndiceTemp = i32lIndiceTemp + 4;
                        blTemp = balBufferTemp[i32lIndiceTemp];
                        //i32ContEspera = 30;
                        Timer1.vfnSetear_Timer(300);
                        if (blTemp == (byte)'1')
                        {
                            eProcesoEstado = procesoEstado.FTP_CID;
                            balBufferTemp = FuncionesDatos.baConvertir_bytes("AT+FTPCID=1\r\n");
                            UartModem.vfnEscribir_Puerto(balBufferTemp, balBufferTemp.Length);
                            eEstadoFTP = Estados.PAUSA_RESPUESTA;
                        }
                        else if (blTemp == (byte)'3')
                        {

                            eProcesoEstado = procesoEstado.FTP_GPRS_ABRIR;
                            balBufferTemp = FuncionesDatos.baConvertir_bytes("AT+SAPBR=1,1\r\n");
                            UartModem.vfnEscribir_Puerto(balBufferTemp, balBufferTemp.Length);
                            eEstadoFTP = Estados.PAUSA_RESPUESTA;
                        }
                        else
                        {
                            balBufferTemp = FuncionesDatos.baConvertir_bytes("AT+SAPBR=2,1\r\n");
                            UartModem.vfnEscribir_Puerto(balBufferTemp, balBufferTemp.Length);
                            eEstadoFTP = Estados.PAUSA_RESPUESTA;
                        }
                    }
                    else
                    {
                        //i32ContEspera = 20;
                        Timer1.vfnSetear_Timer(200);
                        balBufferTemp = FuncionesDatos.baConvertir_bytes("AT+SAPBR=2,1\r\n");
                        UartModem.vfnEscribir_Puerto(balBufferTemp, balBufferTemp.Length);
                        eEstadoFTP = Estados.PAUSA_RESPUESTA;

                    }

                }

#if G120
                Debug.Print("SIMFTP>" + eEstadoFTP.ToString());
#elif PC
                Debug.Print("SIMFTP>" + eEstadoFTP.ToString() + "(1," + i32FTPCodigo + ")");
#endif

                break;

            case Estados.FTP_GPRS_ABRIR:


                if ((i32lTemp = UartModem.CantidadBytes) > 4)
                {

                    balBufferTemp = new byte[i32lTemp];
                    UartModem.i32Leer_Puerto(balBufferTemp, i32lTemp);
                    Timer1.vfnSetear_Timer(300);
                    if (FuncionesDatos.boEncontrar_bytes(balBufferTemp, "ERROR", 0))
                    {

                        balBufferTemp = FuncionesDatos.baConvertir_bytes("AT+SAPBR=3,1,\"APN\",\"internet.itelcel.com\"\r\n");
                        UartModem.vfnEscribir_Puerto(balBufferTemp, balBufferTemp.Length);
                        balBufferTemp = FuncionesDatos.baConvertir_bytes("AT+SAPBR=3,1,\"Contype\",\"GPRS\"\r\n");
                        UartModem.vfnEscribir_Puerto(balBufferTemp, balBufferTemp.Length);
                        //i32ContEspera = 30;
                        eProcesoEstado = procesoEstado.FTP_GPRS_CONFG;
                        eEstadoFTP = Estados.PAUSA_RESPUESTA;

                    }
                    else if (FuncionesDatos.boEncontrar_bytes(balBufferTemp, "OK", 0))
                    {
                        eProcesoEstado = procesoEstado.FTP_GPRS_QUERY;
                        balBufferTemp = FuncionesDatos.baConvertir_bytes("AT+SAPBR=2,1\r\n");
                        //i32ContEspera = 30;
                        UartModem.vfnEscribir_Puerto(balBufferTemp, balBufferTemp.Length);
                        eEstadoFTP = Estados.PAUSA_RESPUESTA;
                    }
                }
#if G120
                Debug.Print("SIMFTP>" + eEstadoFTP.ToString());
#elif PC
                Debug.Print("SIMFTP>" + eEstadoFTP.ToString());
#endif
                break;

            case Estados.FTP_GPRS_CONFG:

                if ((i32lTemp = UartModem.CantidadBytes) > 4)
                {

                    balBufferTemp = new byte[i32lTemp];
                    UartModem.i32Leer_Puerto(balBufferTemp, i32lTemp);
                    if (FuncionesDatos.boEncontrar_bytes(balBufferTemp, "OK", 0))
                    {
                        eProcesoEstado = procesoEstado.FTP_GPRS_ABRIR;
                        balBufferTemp = FuncionesDatos.baConvertir_bytes("AT+SAPBR=1,1\r\n");
                        UartModem.vfnEscribir_Puerto(balBufferTemp, balBufferTemp.Length);
                        //i32ContEspera = 10;

                        Timer1.vfnSetear_Timer(100);
                        eEstadoFTP = Estados.PAUSA_RESPUESTA;
                    }
                    else
                        eEstadoFTP = Estados.INICIO;
                }
                else
                    eEstadoFTP = Estados.ERROR;
#if G120
                Debug.Print("SIMFTP>" + eEstadoFTP.ToString());
#elif PC
                Debug.Print("SIMFTP>" + eEstadoFTP.ToString());
#endif

                break;

            case Estados.PAUSA_RESPUESTA:


                //if(i32ContEspera-- <= 0)
                if (Timer1.dObtener_Timer() <= 0)
                {
                    if (eProcesoEstado == procesoEstado.FTP_GPRS_QUERY)
                        eEstadoFTP = Estados.FTP_GPRS_QUERY;
                    else if (eProcesoEstado == procesoEstado.FTP_GPRS_ABRIR)
                        eEstadoFTP = Estados.FTP_GPRS_ABRIR;
                    else if (eProcesoEstado == procesoEstado.FTP_GPRS_CONFG)
                        eEstadoFTP = Estados.FTP_GPRS_CONFG;
                    else if (eProcesoEstado == procesoEstado.FTP_CID)
                        eEstadoFTP = Estados.FTP_CID;
                    else if (eProcesoEstado == procesoEstado.FTP_SERVIDOR)
                        eEstadoFTP = Estados.FTP_SERVIDOR;
                    else if (eProcesoEstado == procesoEstado.FTP_USUARIO)
                        eEstadoFTP = Estados.FTP_USUARIO;
                    else if (eProcesoEstado == procesoEstado.FTP_CLAVE)
                        eEstadoFTP = Estados.FTP_CLAVE;
                    else if (eProcesoEstado == procesoEstado.FTP_ARCHIVO)
                        eEstadoFTP = Estados.FTP_ARCHIVO;
                    else if (eProcesoEstado == procesoEstado.FTP_DIRECTORIO)
                        eEstadoFTP = Estados.FTP_DIRECTORIO;
                    else if (eProcesoEstado == procesoEstado.FTP_ESCRIBE_DATOS)
                        eEstadoFTP = Estados.FTP_ESCRIBE_DATOS;
                    else if (eProcesoEstado == procesoEstado.FTP_ESC_LEE)
                    {
                        eEstadoFTP = Estados.FTP_IDLE;
                    }
#if G120
                Debug.Print("SIMFTP>" + eEstadoFTP.ToString());
#elif PC
                    Debug.Print("SIMFTP>" + eEstadoFTP.ToString());
#endif
                }

                break;

            case Estados.FTP_CID:

                if ((i32lTemp = UartModem.CantidadBytes) > 4)
                {
                    balBufferTemp = new byte[i32lTemp];
                    UartModem.i32Leer_Puerto(balBufferTemp, i32lTemp);

                    //string mansaje = new string(UTF8Encoding.UTF8.GetChars(balBufferTemp));

                    if (FuncionesDatos.boEncontrar_bytes(balBufferTemp, "OK", 0))
                    {

                        balBufferTemp = FuncionesDatos.baConvertir_bytes("AT+FTPSERV=\"" + sServidor + "\"\r\n");
                        UartModem.vfnEscribir_Puerto(balBufferTemp, balBufferTemp.Length);
                        eProcesoEstado = procesoEstado.FTP_SERVIDOR;
                        //i32ContEspera = 50;

                        Timer1.vfnSetear_Timer(500);
                        eEstadoFTP = Estados.PAUSA_RESPUESTA;
                    }
                    else
                    {
                        balBufferTemp = FuncionesDatos.baConvertir_bytes("AT+SAPBR=0,1\r\n");
                        UartModem.vfnEscribir_Puerto(balBufferTemp, balBufferTemp.Length);
                        eEstadoFTP = Estados.FTP_FINALIZADO;
                    }
                }
                else
                    eEstadoFTP = Estados.ERROR;
#if G120
                Debug.Print("SIMFTP>" + eEstadoFTP.ToString());
#elif PC
                Debug.Print("SIMFTP>" + eEstadoFTP.ToString());
#endif
                break;

            case Estados.FTP_SERVIDOR:

                balBufferTemp = FuncionesDatos.baConvertir_bytes("AT+FTPUN=\"" + sUsuario + "\"\r\n");
                UartModem.vfnEscribir_Puerto(balBufferTemp, balBufferTemp.Length);
                eProcesoEstado = procesoEstado.FTP_USUARIO;
                //i32ContEspera = 20;
                Timer1.vfnSetear_Timer(200);
                eEstadoFTP = Estados.PAUSA_RESPUESTA;
#if G120
                Debug.Print("SIMFTP>" + eEstadoFTP.ToString());
#elif PC
                Debug.Print("SIMFTP>" + eEstadoFTP.ToString());
#endif

                break;

            case Estados.FTP_USUARIO:

                balBufferTemp = FuncionesDatos.baConvertir_bytes("AT+FTPPW=\"" + sClave + "\"\r\n");
                UartModem.vfnEscribir_Puerto(balBufferTemp, balBufferTemp.Length);
                eProcesoEstado = procesoEstado.FTP_CLAVE;
                //i32ContEspera = 20;
                Timer1.vfnSetear_Timer(200);
                eEstadoFTP = Estados.PAUSA_RESPUESTA;
#if G120
                Debug.Print("SIMFTP>" + eEstadoFTP.ToString());
#elif PC
                Debug.Print("SIMFTP>" + eEstadoFTP.ToString() + "(1," + i32FTPCodigo + ")");
#endif

                break;

            case Estados.FTP_CLAVE:

                if (eTipoProceso == TipoProceso.ESCRITURA)
                {
                    balBufferTemp = FuncionesDatos.baConvertir_bytes("AT+FTPPUTNAME=\"" + sArchivo + "\"\r\n");
                }
                else
                {
                    balBufferTemp = FuncionesDatos.baConvertir_bytes("AT+FTPGETNAME=\"" + sArchivo + "\"\r\n");
                }
                UartModem.vfnEscribir_Puerto(balBufferTemp, balBufferTemp.Length);
                eProcesoEstado = procesoEstado.FTP_ARCHIVO;
                //i32ContEspera = 20;
                Timer1.vfnSetear_Timer(200);
                eEstadoFTP = Estados.PAUSA_RESPUESTA;
#if G120
                Debug.Print("SIMFTP>" + eEstadoFTP.ToString());
#elif PC
                Debug.Print("SIMFTP>" + eEstadoFTP.ToString());
#endif

                break;

            case Estados.FTP_ARCHIVO:

                if (eTipoProceso == TipoProceso.ESCRITURA)
                {
                    balBufferTemp = FuncionesDatos.baConvertir_bytes("AT+FTPPUTPATH=\"" + sDirectorio + "\"\r\n");
                }
                else
                {
                    balBufferTemp = FuncionesDatos.baConvertir_bytes("AT+FTPGETPATH=\"" + sDirectorio + "\"\r\n");
                }
                UartModem.vfnEscribir_Puerto(balBufferTemp, balBufferTemp.Length);
                eProcesoEstado = procesoEstado.FTP_DIRECTORIO;
                //i32ContEspera = 20;
                Timer1.vfnSetear_Timer(200);
                eEstadoFTP = Estados.PAUSA_RESPUESTA;
#if G120
                Debug.Print("SIMFTP>" + eEstadoFTP.ToString());
#elif PC
                Debug.Print("SIMFTP>" + eEstadoFTP.ToString());
#endif

                break;

            case Estados.FTP_DIRECTORIO:


                eProcesoEstado = procesoEstado.FTP_ESC_LEE;
                eEstadoFTP = Estados.PAUSA_RESPUESTA;
                if (eTipoTrans == TipoFTP.ESCRITURA)
                {
                    balBufferTemp = FuncionesDatos.baConvertir_bytes("AT+FTPPUT=1\r\n");
                }
                else
                {
                    balBufferTemp = FuncionesDatos.baConvertir_bytes("AT+FTPGET=1\r\n");

                    if (eTipoAlmace == tipoAlmacenamiento.SD)
                    {
                        try
                        {
                            fsArchivo = new FileStream(sDirectorioSD + sArchivoSD, FileMode.Create, FileAccess.Write);
                        }
                        catch (Exception em)
                        {

                            i32FTPCodError = -3;
                            eEstadoFTP = Estados.ERROR;
                            
                        }

                    }

                }
                UartModem.vfnLimpiar_Puerto();
                UartModem.vfnEscribir_Puerto(balBufferTemp, balBufferTemp.Length);
                i32IndiceBuffer = 0;
                i32IndiceBufferRec = 0;
                i32IndiceBufferTx = 0;
                Timer2.vfnSetear_Timer(60000);
                Timer1.vfnSetear_Timer(200);
                

#if G120
                Debug.Print("SIMFTP>" + eEstadoFTP.ToString());
#elif PC
                Debug.Print("SIMFTP>" + eEstadoFTP.ToString());
#endif

                break;

            case Estados.FTP_IDLE:

                if (UartModem.CantidadBytes > 0)
                {
                    if (eTipoTrans == TipoFTP.ESCRITURA)
                    {
                        eEstadoFTP = Estados.FTP_IDLE_ESCRITURA;
                    }
                    else
                    {
                        eEstadoFTP = Estados.FTP_IDLE_LECTURA;
                    }
                    Timer2.vfnSetear_Timer(60000);
                }
                if (Timer2.dObtener_Timer() <= 0)
                {
                    i32FTPCodError = -2;
                    eEstadoFTP = Estados.FTP_FINALIZADO;
                }

                break;

            case Estados.FTP_IDLE_LECTURA:

                if ((i32lTemp = UartModem.CantidadBytes) > 0)
                {
                    balBufferTemp = new byte[i32lTemp];
                    UartModem.i32Leer_Puerto(balBufferTemp, i32lTemp);
                    Array.Copy(balBufferTemp, 0, baBufferRec, i32IndiceBufferRec, i32lTemp);

                    i32IndiceBufferRec = i32IndiceBufferRec + i32lTemp;
                    i32LargoBufferRec = i32IndiceBufferRec;
                    //i32ContEspera = 20;
                    Timer1.vfnSetear_Timer(20);

                }
                else
                {
                    if (Timer1.dObtener_Timer() <= 0)
                    {
                        i32IndiceBufferRec = 0;
                        i32lIndiceTemp = 0;
                        eEstadoFTP = Estados.FTP_ANA_LECT;
                    }
                }

                break;

            case Estados.FTP_ANA_LECT:

                eEstadoFTP = Estados.FTP_IDLE;
                //i32ContEspera = 20;
                //i32lIndiceTemp = 0;
                //Timer1.vfnSetear_Timer(50);
                if (i32LargoBufferRec > 0)
                {
                    //balBufferTemp = new byte[i32LargoBufferRec];

                    //Array.Copy(baBufferRec, i32IndiceBufferRec, balBufferTemp, 0, i32LargoBufferRec);

                    //string mensaje = new string(UTF8Encoding.UTF8.GetChars(balBufferTemp));

                    do
                    {
                        if (baBufferRec[i32lIndiceTemp] == (byte)'+')
                        {
                            i32lIndiceTemp = i32lIndiceTemp + 1;
                            i32lTemp = FuncionesDatos.i32Encontrar_IndiceFinalExtricto(baBufferRec, "FTPGET:", i32lIndiceTemp);
                            if (i32lTemp >= 0)
                            {
                                i32lIndiceTemp = i32lTemp;
                                i32lIndiceTemp = i32lIndiceTemp + 1;
                                if (baBufferRec[i32lIndiceTemp] == (byte)'1')
                                {

                                    i32lIndiceTemp = i32lIndiceTemp + 1;
                                    i32lTemp = Array.IndexOf(baBufferRec, (byte)',', i32lIndiceTemp);
                                    i32lIndiceTemp = i32lTemp;
                                    i32lTemp = Array.IndexOf(baBufferRec, (byte)'\r', i32lIndiceTemp + 1) - i32lTemp - 1;

                                    if (i32lTemp > 0 && i32lTemp < 5)
                                    {
                                        i32lIndiceTemp = i32lIndiceTemp + 1;
                                        //i32FTPCodError = 0;
                                        i32FTPCodigo = 0;
                                        for (int i = 0; i < i32lTemp; i++)
                                        {

                                            blTemp = (byte)(baBufferRec[i32lIndiceTemp + i] - 0x30);
                                            if (blTemp >= 0 && blTemp <= 9)
                                            {
                                                i32FTPCodigo = i32FTPCodigo + ((int)System.Math.Pow(10, i32lTemp - 1 - i) * blTemp);
                                            }
                                            else
                                            {
                                                i32FTPCodigo = -1;
                                                break;
                                            }
                                        }
                                        
                                    }
                                    if (i32FTPCodigo >= 0)
                                    {
                                        if (i32FTPCodigo == 1)
                                        {
                                            eEstadoFTP = Estados.LEER_FTP;
                                        }
                                        else if (i32FTPCodigo == 2)
                                        {
                                            i32FTPCodError = 0;
                                            eEstadoFTP = Estados.FTP_FINALIZADO;
                                        }
                                        else
                                        {
                                            
                                            i32FTPCodError = i32FTPCodigo;
                                            eErrorFTP = (mensajeError)i32FTPCodError;
                                            eEstadoFTP = Estados.ERROR;
                                        }
                                    }


                                    i32lIndiceTemp = i32lIndiceTemp + i32lTemp;

#if G120
                                    Debug.Print("SIMFTP>" + eEstadoFTP.ToString() + " (1," + i32FTPCodigo.ToString() + ")");
#elif PC
                                    Debug.Print("SIMFTP>" + eEstadoFTP.ToString() + " (1," + i32FTPCodigo.ToString() +")");
#endif
                                }
                                else if (baBufferRec[i32lIndiceTemp] == (byte)'2')
                                {

                                    i32lIndiceTemp = i32lIndiceTemp + 1;
                                    i32lTemp = Array.IndexOf(baBufferRec, (byte)',', i32lIndiceTemp);
                                    i32lIndiceTemp = i32lTemp;
                                    i32lTemp = Array.IndexOf(baBufferRec, (byte)'\r', i32lIndiceTemp + 1) - i32lTemp - 1;

                                    if (i32lTemp > 0 && i32lTemp < 5)
                                    {
                                        i32lIndiceTemp = i32lIndiceTemp + 1;

                                        for (int i = 0; i < i32lTemp; i++)
                                        {

                                            blTemp = (byte)(baBufferRec[i32lIndiceTemp + i] - 0x30);
                                            if (blTemp >= 0 && blTemp <= 9)
                                            {
                                                i32lCantDatos = i32lCantDatos + (blTemp * (int)System.Math.Pow(10, i32lTemp - 1 - i));
                                            }
                                            else
                                            {
                                                i32lCantDatos = -1;
                                                break;
                                            }
                                        }

                                        if (i32lCantDatos > 0)
                                        {
                                            if (eTipoAlmace == tipoAlmacenamiento.RAM)
                                            {
                                                i32lIndiceTemp = i32lIndiceTemp + 2;
                                                Array.Copy(baBufferRec, i32lIndiceTemp, baBuffer, i32IndiceBuffer, i32lCantDatos);
                                                i32IndiceBuffer = i32IndiceBuffer + i32lCantDatos;
                                            }
                                            else
                                            {
                                                i32lIndiceTemp = i32lIndiceTemp + 2;
                                                try
                                                {
                                                    fsArchivo.Write(baBufferRec, i32lIndiceTemp, i32lCantDatos);
                                                }
                                                catch
                                                {
#if G120
                                                    Debug.Print("SIMFTP>" + eEstadoFTP.ToString() + " (2) Error Escritura");
#elif PC
                                                    Debug.Print("SIMFTP>" + eEstadoFTP.ToString() + " (2) Error Escritura");
#endif

                                                }
                                            }
                                            i32lIndiceTemp = i32lIndiceTemp + i32lCantDatos;
                                            eEstadoFTP = Estados.LEER_FTP;
                                        }
                                        i32lIndiceTemp = i32lIndiceTemp + i32lTemp;
#if G120
                                        Debug.Print("SIMFTP>" + eEstadoFTP.ToString() + " (2," + i32lCantDatos.ToString() + ")");
#elif PC
                                        Debug.Print("SIMFTP>" + eEstadoFTP.ToString() + " (2," + i32lCantDatos.ToString() + ")");
#endif
                                    }

                                }

                            }
                            else
                            {
                                i32lIndiceTemp++;
                            }

                        }
                        else
                        {
                            i32lIndiceTemp++;
                        }
                    } while (i32lIndiceTemp <= i32LargoBufferRec);

                    Array.Clear(baBufferRec, 0x00, baBufferRec.Length);
                    i32IndiceBufferRec = 0;

                }

                //i32IndiceBufferRec = 0;
                //Array.Clear(baBufferRec, 0, baBufferRec.Length);
#if G120
                Debug.Print("SIMFTP>" + eEstadoFTP.ToString());
#elif PC
                Debug.Print("SIMFTP>" + eEstadoFTP.ToString());
#endif
                break;

            

            case Estados.LEER_FTP:

                balBufferTemp = FuncionesDatos.baConvertir_bytes("AT+FTPGET=2,1024\r\n");
                UartModem.vfnEscribir_Puerto(balBufferTemp, balBufferTemp.Length);

                eEstadoFTP = Estados.FTP_IDLE;

#if G120
                //Debug.Print("SIMFTP>" + eEstadoFTP.ToString());
#endif
                break;


            case Estados.FTP_FINALIZADO:

                stSimcomFTP.i32CantDatos = i32IndiceBuffer;
                //Console.Write( "Error:" + i32FTPCodError.ToString());
                //Debug.Print("Error: " + i32FTPCodError.ToString());
                eProcesoEstado = procesoEstado.FTP_FINALIZADO;
                stSimcomFTP.boRFlagProcesoOcupado = false;
                if (eTipoAlmace == tipoAlmacenamiento.SD)
                {
                    if (fsArchivo != null)
                    {
                        fsArchivo.Close();
                        fsArchivo.Dispose();
                    }
                    fsArchivo = null;
                }
                eTipoProceso = TipoProceso.NINGUNO;
                eEstadoFTP = Estados.INACTIVO;
#if G120
                Debug.Print("SIMFTP>" + eEstadoFTP.ToString());
#endif

                break;

            case Estados.FTP_IDLE_ESCRITURA:

                if ((i32lTemp = UartModem.CantidadBytes) > 0)
                {
                    balBufferTemp = new byte[i32lTemp];
                    UartModem.i32Leer_Puerto(balBufferTemp, i32lTemp);
                    Array.Copy(balBufferTemp, 0, baBufferRec, i32IndiceBufferRec, i32lTemp);

                    i32IndiceBufferRec = i32IndiceBufferRec + i32lTemp;
                    i32LargoBufferRec = i32IndiceBufferRec;

                    //i32ContEspera = 20;
                    Timer1.vfnSetear_Timer(20);

                    //Timer2.vfnSetear_Timer(000);
                }
                else
                {
                    if (Timer1.dObtener_Timer() <= 0)
                    {
                        i32IndiceBufferRec = 0;
                        i32lIndiceTemp = 0;
                        eEstadoFTP = Estados.FTP_ANA_ESC;
                    }
                }


                break;

            case Estados.FTP_ANA_ESC:

                eEstadoFTP = Estados.FTP_IDLE;
                //i32ContEspera = 30;

                if (i32LargoBufferRec > 0)
                {
                    //balBufferTemp = new byte[i32LargoBufferRec];
                    //Array.Copy(baBufferRec, i32IndiceBufferRec, balBufferTemp, 0, i32LargoBufferRec);
                    //string mensaje = new string(UTF8Encoding.UTF8.GetChars(balBufferTemp));
                    do
                    {

                        if (baBufferRec[i32lIndiceTemp] == (byte)'+')
                        {
                            i32lIndiceTemp = i32lIndiceTemp + 1;
                            i32lTemp = FuncionesDatos.i32Encontrar_IndiceFinalExtricto(baBufferRec, "FTPPUT:", i32lIndiceTemp);
                            if (i32lTemp >= 0)
                            {
                                i32lIndiceTemp = i32lTemp + 1;
                                if (baBufferRec[i32lIndiceTemp] == (byte)'1')
                                {
                                    i32lIndiceTemp = i32lIndiceTemp + 1;
                                    i32lTemp = Array.IndexOf(baBufferRec, (byte)',', i32lIndiceTemp);
                                    i32lIndiceTemp = i32lTemp;
                                    i32lTemp = Array.IndexOf(baBufferRec, (byte)'\r', i32lIndiceTemp + 1);

                                    if(i32lTemp-i32lIndiceTemp > 2)
                                        i32lTemp = Array.IndexOf(baBufferRec, (byte)',', i32lIndiceTemp + 1);
                                    i32lTemp = i32lTemp - i32lIndiceTemp - 1;
                                    if (i32lTemp > 0 && i32lTemp < 5)
                                    {
                                        i32lIndiceTemp = i32lIndiceTemp + 1;
                                        //i32FTPCodError = 0;
                                        i32FTPCodigo = 0;
                                        for (int i = 0; i < i32lTemp; i++)
                                        {

                                            blTemp = (byte)(baBufferRec[i32lIndiceTemp + i] - 0x30);
                                            if (blTemp >= 0 && blTemp <= 9)
                                            {
                                                i32FTPCodigo = i32FTPCodigo + ((int)System.Math.Pow(10, i32lTemp - 1 - i) * blTemp);
                                            }
                                            else
                                            {
                                                i32FTPCodigo = -1;
                                                break;
                                            }
                                        }

                                        if (i32FTPCodigo >= 0)
                                        {
                                            if (i32FTPCodigo == 1)
                                            {

                                                eEstadoFTP = Estados.ESCRIBIR_FTP;
                                            }
                                            else if (i32FTPCodigo == 0)
                                            {
                                                i32FTPCodError = 0;
                                                eEstadoFTP = Estados.FTP_FINALIZADO;
                                            }
                                            else
                                            {
                                                i32FTPCodError = i32FTPCodigo;
                                                eErrorFTP = (mensajeError)i32FTPCodError;
                                                eEstadoFTP = Estados.ERROR;
                                            }
                                        }
                                        i32lIndiceTemp = i32lIndiceTemp + i32lTemp;
                                    }

#if G120
                Debug.Print("SIMFTP>" + eEstadoFTP.ToString());
#elif PC
                                    Debug.Print("SIMFTP>" + eEstadoFTP.ToString() + "(1," + i32FTPCodigo + ")");
#endif

                                }

                            }

                        }
                        else
                            i32lIndiceTemp++;


                    } while (i32lIndiceTemp <= i32LargoBufferRec);

                    Array.Clear(baBufferRec, 0x00, baBufferRec.Length);
                    i32IndiceBufferRec = 0;

                }
                //i32IndiceBufferRec = 0;
                //Array.Clear(baBufferRec, 0, baBufferRec.Length);
#if G120
                Debug.Print("SIMFTP>" + eEstadoFTP.ToString());
#elif PC
                Debug.Print("SIMFTP>" + eEstadoFTP.ToString());
#endif
                break;

            case Estados.ESCRIBIR_FTP:

                i32lTemp = stSimcomFTP.i32CantDatos - i32IndiceBufferTx;
                if (i32lTemp > 1024)
                {
                    i32lTemp = 1024;
                }
                if (i32lTemp >= 0)
                {

                    baBufferTra = new byte[i32lTemp];
                    Array.Copy(baBuffer, i32IndiceBufferTx, baBufferTra, 0, i32lTemp);
                    i32IndiceBufferTx = i32lTemp;
                    balBufferTemp = FuncionesDatos.baConvertir_bytes("AT+FTPPUT=2," + i32lTemp.ToString() + "\r\n");
                    UartModem.vfnEscribir_Puerto(balBufferTemp, balBufferTemp.Length);
                }

                eProcesoEstado = procesoEstado.FTP_ESCRIBE_DATOS;
                //i32ContEspera = 100;
                Timer1.vfnSetear_Timer(1000);
                eEstadoFTP = Estados.PAUSA_RESPUESTA;
#if G120
                Debug.Print("SIMFTP>" + eEstadoFTP.ToString() + " Cant: " + i32lTemp.ToString());
#elif PC
                Debug.Print("SIMFTP>" + eEstadoFTP.ToString() + " Cant: " + i32lTemp.ToString());
#endif
                break;

            case Estados.FTP_ESCRIBE_DATOS:


                if (baBufferTra.Length > 0)
                {
                    UartModem.vfnEscribir_Puerto(baBufferTra, baBufferTra.Length);
                }

                eEstadoFTP = Estados.FTP_IDLE;
#if G120
                Debug.Print("SIMFTP>" + eEstadoFTP.ToString());
#endif
                break;

            case Estados.ERROR:

                eEstadoFTP = Estados.FTP_FINALIZADO;
#if G120
                Debug.Print("SIMFTP>" + eEstadoFTP.ToString() + " ERROR");
#endif
                break;
        }
    }
}




public struct stSimcomFTP
{
    static public bool boRFlagProcesoOcupado;
    static internal int i32CantDatos;

    static public bool boPreparar_RecepcionDirFTP(string sArchivoFTP, string sDirectorioFTP, string sArchivoSD, string sDirectorioSD)
    {
        if (boRFlagProcesoOcupado == false)
        {

            SimcomFTP.sArchivo = sArchivoFTP;
            SimcomFTP.sDirectorio = sDirectorioFTP;
            SimcomFTP.sArchivoSD = sArchivoSD;
            SimcomFTP.sDirectorioSD = sDirectorioSD;
            SimcomFTP.eTipoAlmace = SimcomFTP.tipoAlmacenamiento.SD;
            SimcomFTP.eTipoProceso = TipoProceso.LECTURA;
            boRFlagProcesoOcupado = true;
            return true;
        }
        return false;
    }

    static public bool boPreparar_EnvioRecepcionFTP(TipoFTP eTipoFTP, string sArchivo, string sDirectorio)
    {
        if (boRFlagProcesoOcupado == false)
        {

            SimcomFTP.sArchivo = sArchivo;
            SimcomFTP.sDirectorio = sDirectorio;
            SimcomFTP.eTipoAlmace = SimcomFTP.tipoAlmacenamiento.RAM;
            if (eTipoFTP == TipoFTP.LECTURA)
            {
                SimcomFTP.eTipoProceso = TipoProceso.LECTURA;
                boRFlagProcesoOcupado = true;

            }
            else
            {
                SimcomFTP.eTipoProceso = TipoProceso.ESCRITURA;
            }
            return true;
        }
        return false;
    }

    static public void vfnEnviar_Datos(byte[] baDatos, int i32Cantidad)
    {
        boRFlagProcesoOcupado = true;
        i32CantDatos = i32Cantidad;
        Array.Copy(baDatos, SimcomFTP.baBuffer, i32Cantidad);
    }

    static public int i32Recibir_Datos(byte[] baDatos, int i32Cantidad)
    {
        int i32lTemp = i32CantDatos;
        Array.Copy(SimcomFTP.baBuffer, baDatos, i32CantDatos);
        i32CantDatos = 0;
        return i32lTemp;
    }

    static public int i32Leer_CantidadDatos()
    {
        return i32CantDatos;
    }

    static public string sObtener_Proceso()
    {
        return SimcomFTP.eProcesoEstado.ToString();
    }

    static public int s32Obtener_CodigoError()
    {
        return SimcomFTP.i32FTPCodError;
    }

    static public string sObtener_MensajeError()
    {
        return SimcomFTP.eErrorFTP.ToString();
    }

}