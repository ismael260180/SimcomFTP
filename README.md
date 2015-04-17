# SimcomFTP

el proceso de la Maquina de estados puedes ser leido para NETMF se obtendra el valor decimal en cadena de texto 
y para PC el nombre del proceso.

procesos

        INACTIVO,                //0
        FTP_GPRS_QUERY,         //1     
        FTP_GPRS_ABRIR,         //2     
        FTP_GPRS_CONFG,         //3
        FTP_CID,                //4
        FTP_SERVIDOR,           //5
        FTP_USUARIO,            //6
        FTP_CLAVE,              //7
        FTP_ARCHIVO,            //8
        FTP_DIRECTORIO,         //9
        FTP_ESC_LEE,            //10
        FTP_ESCRIBE_DATOS,      //11
        FTP_FINALIZADO,         //12

para revisar si el proceso del FTP fue exitoso o no podemos hacer esta validacion:

        if (stSimcomFTP.sObtener_Proceso() == "12")
        {
            boFlagSalida = true;
            s32Temp = stSimcomFTP.s32Obtener_CodigoError();
            if (s32Temp < 0)
            {
                Debug.Print("Error de Secuencia: " + s32Temp.ToString() + "\r");
            }
            else if (s32Temp > 0)
            {
                Debug.Print("Error FTP: " + stSimcomFTP.sObtener_MensajeError() + "\r");
            }
            else
            {
                Debug.Print("FTP Exitoso " + stSimcomFTP.sObtener_MensajeError() + " Error\r");
            }
        }
