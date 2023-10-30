Imports System.Management
Imports Microsoft.Win32
Imports System.IO
Imports System.Security.AccessControl

Public Class frmCompartir

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        Dim ruta As String = txtRuta.Text
        Dim result As String = ""

        result &= CompartirCarpeta(ruta)
        result &= ConfigurarPermisosCompartidos(ruta)
        result &= ConfigurarPermisosSeguridad(ruta)
        DarControlTotalATodos(ruta)
        DesactivarSolicitudesCredenciales()
        DesactivarUsoCompartidoConProteccionPorContrasena()

        MsgBox("Se ha compartido la carpeta correctamente")


        MsgBox("Cambios realizados:" & vbCrLf & result)
    End Sub


    Private Function CompartirCarpeta(ruta As String) As String
        Return RunProcessAndGetOutput("net", "share " & Path.GetFileName(ruta) & "=" & ruta)
    End Function

    Private Function ConfigurarPermisosCompartidos(ruta As String) As String
        Return RunProcessAndGetOutput("icacls", ruta & " /grant Todos:(OI)(CI)M")
    End Function

    Private Function ConfigurarPermisosSeguridad(ruta As String) As String
        Return RunProcessAndGetOutput("icacls", ruta & " /grant *S-1-1-0:(OI)(CI)F")
    End Function


    Private Sub DarControlTotalATodos(ruta As String)
        Try
            Dim carpetaInfo As New DirectoryInfo(ruta)
            Dim acl As DirectorySecurity = carpetaInfo.GetAccessControl()
            Dim rule As New FileSystemAccessRule("Todos", FileSystemRights.FullControl, InheritanceFlags.ContainerInherit Or InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow)
            acl.AddAccessRule(rule)
            carpetaInfo.SetAccessControl(acl)
        Catch ex As Exception
            MsgBox("Error al modificar los permisos: " & ex.Message)
        End Try
    End Sub


    ' Desactivar el uso compartido con protección por contraseña
    Public Sub DesactivarSolicitudesCredenciales()
        Try
            Dim scope As New ManagementScope("\\.\root\cimv2")
            Dim query As New ObjectQuery("SELECT * FROM Win32_Service WHERE Name='LanmanServer'")

            Using searcher As New ManagementObjectSearcher(scope, query)
                Dim services As ManagementObjectCollection = searcher.Get()

                For Each service As ManagementObject In services
                    Dim args() As Object = {Nothing}
                    service.InvokeMethod("ChangeStartMode", args)

                    Dim inParams As ManagementBaseObject = service.GetMethodParameters("ChangeStartMode")
                    inParams("StartMode") = 2 ' Automatic start
                    service.InvokeMethod("ChangeStartMode", inParams, Nothing)
                Next
            End Using

            ' Desactivar el uso compartido con protección por contraseña
            Dim sharingParametersPath As String = "SYSTEM\CurrentControlSet\Services\LanmanServer\Parameters"
            Dim sharingParameters As RegistryKey = Registry.LocalMachine.OpenSubKey(sharingParametersPath, True)

            If sharingParameters IsNot Nothing Then
                sharingParameters.SetValue("restrictnullsessaccess", 0)
            Else
                MsgBox("La clave del Registro 'restrictnullsessaccess' no se encontró en la ubicación especificada.")
            End If
        Catch ex As Exception
            ' Manejo de excepciones: muestra un mensaje de error en caso de que ocurra una excepción.
            MsgBox("Error al desactivar las solicitudes de credenciales: " & ex.Message)
        End Try
    End Sub


    'Public Sub DesactivarUsoCompartidoConProteccionPorContrasena3()
    '    Try
    '        Dim scope As New ManagementScope("\\.\root\cimv2")
    '        Dim query As New ObjectQuery("SELECT * FROM Win32_ShareSecuritySetting")

    '        Using searcher As New ManagementObjectSearcher(scope, query)
    '            Dim shares As ManagementObjectCollection = searcher.Get()

    '            For Each share As ManagementObject In shares
    '                If share("SecurityDescriptor") IsNot Nothing Then
    '                    Dim securityDescriptor As ManagementBaseObject = DirectCast(share("SecurityDescriptor"), ManagementBaseObject)
    '                    Dim controlFlags As Integer = CInt(securityDescriptor("ControlFlags"))

    '                    ' Desactivar la protección por contraseña (quitar el bit 0x02)
    '                    controlFlags = controlFlags And Not &H2
    '                    securityDescriptor("ControlFlags") = controlFlags
    '                    'securityDescriptor.Put()
    '                End If
    '            Next
    '        End Using

    '        MsgBox("El uso compartido con protección por contraseña se ha desactivado correctamente.")

    '    Catch ex As Exception
    '        ' Manejo de excepciones: muestra un mensaje de error en caso de que ocurra una excepción.
    '        MsgBox("Error al desactivar el uso compartido con protección por contraseña: " & ex.Message)
    '    End Try
    'End Sub


    Public Sub VerificarExistenciaClaseWMI()
        Try
            Dim scope As New ManagementScope("\\.\root\cimv2")
            Dim query As New ObjectQuery("SELECT * FROM meta_class")

            Using searcher As New ManagementObjectSearcher(scope, query)
                Dim classes As ManagementObjectCollection = searcher.Get()

                Dim claseBuscada As String = "Win32_ShareSecuritySetting"
                Dim claseEncontrada As Boolean = False

                For Each classObject As ManagementObject In classes
                    Dim className As String = CStr(classObject("Name"))

                    If className = claseBuscada Then
                        claseEncontrada = True
                        Exit For
                    End If
                Next

                If claseEncontrada Then
                    MsgBox("La clase 'Win32_ShareSecuritySetting' está disponible en el sistema.")
                Else
                    MsgBox("La clase 'Win32_ShareSecuritySetting' no se encontró en el sistema.")
                End If
            End Using

        Catch ex As Exception
            ' Manejo de excepciones: muestra un mensaje de error en caso de que ocurra una excepción.
            MsgBox("Error al verificar la existencia de la clase: " & ex.Message)
        End Try
    End Sub


    '***********************************************************************
    Public Sub DesactivarUsoCompartidoConProteccionPorContrasena()
        Try
            VerificarExistenciaClaseWMI()
            Dim scope As New ManagementScope("\\.\root\cimv2")
            Dim query As New ObjectQuery("SELECT * FROM Win32_ShareSecuritySetting")

            Using searcher As New ManagementObjectSearcher(scope, query)
                Dim shares As ManagementObjectCollection = searcher.Get()

                For Each share As ManagementObject In shares
                    If share("SecurityDescriptor") IsNot Nothing Then
                        Dim securityDescriptor As ManagementObject = DirectCast(share("SecurityDescriptor"), ManagementObject)
                        Dim controlFlags As Integer = CInt(securityDescriptor("ControlFlags"))

                        ' Desactivar la protección por contraseña (quitar el bit 0x02)
                        controlFlags = controlFlags And Not &H2
                        securityDescriptor("ControlFlags") = controlFlags
                        securityDescriptor.Put()
                    End If
                Next
            End Using

            MsgBox("El uso compartido con protección por contraseña se ha desactivado correctamente.")

        Catch ex As Exception
            ' Manejo de excepciones: muestra un mensaje de error en caso de que ocurra una excepción.
            MsgBox("Error al desactivar el uso compartido con protección por contraseña: " & ex.Message)
        End Try
    End Sub




    Public Sub RevertirCambios(ruta As String)
        Try
            ' Eliminar el recurso compartido de la carpeta
            Dim eliminarCompartido As String = "net share " & Path.GetFileName(ruta) & " /delete"
            RunProcessAndGetOutput("cmd", "/c " & eliminarCompartido)

            ' Restablecer permisos de compartición
            Dim restablecerPermisosCompartidos As String = "icacls " & ruta & " /remove:d Todos"
            RunProcessAndGetOutput("cmd", "/c " & restablecerPermisosCompartidos)

            ' Restablecer permisos de seguridad
            Dim restablecerPermisosSeguridad As String = "icacls " & ruta & " /remove:g *S-1-1-0"
            RunProcessAndGetOutput("cmd", "/c " & restablecerPermisosSeguridad)

            ' Restablecer permisos de control total
            Dim carpetaInfo As New DirectoryInfo(ruta)
            Dim acl As DirectorySecurity = carpetaInfo.GetAccessControl()
            Dim rule As New FileSystemAccessRule("Todos", FileSystemRights.FullControl, AccessControlType.Deny)
            acl.RemoveAccessRule(rule)
            carpetaInfo.SetAccessControl(acl)

            ' Restablecer el uso compartido con protección por contraseña
            Dim sharingParametersPath As String = "SYSTEM\CurrentControlSet\Services\LanmanServer\Parameters"
            Dim sharingParameters As RegistryKey = Registry.LocalMachine.OpenSubKey(sharingParametersPath, True)

            If sharingParameters IsNot Nothing Then
                sharingParameters.DeleteValue("restrictnullsessaccess", False)
            End If

            MsgBox("Cambios revertidos con éxito.")
        Catch ex As Exception
            ' Manejo de excepciones: muestra un mensaje de error en caso de que ocurra una excepción.
            MsgBox("Error al revertir los cambios: " & ex.Message)
        End Try
    End Sub


    Private Sub btnRevertir_Click(sender As Object, e As EventArgs) Handles btnRevertir.Click
        RevertirCambios(txtRuta.Text)

    End Sub



    Private Function RunProcessAndGetOutput(filename As String, arguments As String) As String
        Try
            Dim proceso As New Process()
            proceso.StartInfo.FileName = filename
            proceso.StartInfo.Arguments = arguments
            proceso.StartInfo.RedirectStandardOutput = True
            proceso.StartInfo.UseShellExecute = False
            proceso.StartInfo.CreateNoWindow = True
            proceso.Start()
            Dim output As String = proceso.StandardOutput.ReadToEnd()
            proceso.WaitForExit()

            Return output
        Catch ex As Exception
            ' Manejo de excepciones: muestra un mensaje de error en caso de que ocurra una excepción.
            MsgBox("Error al ejecutar el proceso: " & ex.Message)
            Return ""
        End Try
    End Function


    Public Sub DesactivarUsoCompartidoConProteccionPorContrasena2()
        Try
            ' Abre la clave del Registro relacionada con la configuración avanzada de uso compartido
            Dim advancedSharingSettings As RegistryKey = Registry.CurrentUser.OpenSubKey("Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", True)

            ' Verifica si la clave existe; si no, créala
            If advancedSharingSettings Is Nothing Then
                advancedSharingSettings = Registry.CurrentUser.CreateSubKey("Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", RegistryKeyPermissionCheck.ReadWriteSubTree)
            End If

            ' Establece el valor de restricción de sesión nula (restrictnullsessaccess) en 0 para desactivar el uso compartido con protección por contraseña
            advancedSharingSettings.SetValue("restrictnullsessaccess", 0, RegistryValueKind.DWord)

            ' Cierra la clave del Registro
            advancedSharingSettings.Close()

            MsgBox("El uso compartido con protección por contraseña se ha desactivado correctamente.")

        Catch ex As Exception
            ' Manejo de excepciones: muestra un mensaje de error en caso de que ocurra una excepción.
            MsgBox("Error al desactivar el uso compartido con protección por contraseña: " & ex.Message)
        End Try
    End Sub

    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click
        Me.Close()
    End Sub

    Private Sub frmCompartir_Load(sender As Object, e As EventArgs) Handles MyBase.Load

    End Sub

End Class