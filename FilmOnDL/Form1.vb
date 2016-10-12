Imports System.Net
Imports System.IO
Imports System.Text
Imports System.Text.RegularExpressions

Public Class Form1

    Dim SessionKey As String

    Private Function SendRequest(uri As Uri, jsonDataBytes As Byte(), contentType As String, method As String) As String
        'Post JSON to URL and get response
        Dim req As HttpWebRequest = WebRequest.Create(uri)
        req.ContentType = contentType
        req.Method = method
        req.ContentLength = jsonDataBytes.Length
        req.UserAgent = "Mozilla/5.0 (Windows NT 6.3; rv:36.0) Gecko/20100101 Firefox/36.0"

        Dim stream = req.GetRequestStream()
        stream.Write(jsonDataBytes, 0, jsonDataBytes.Length)
        stream.Close()

        Dim response = req.GetResponse().GetResponseStream()


        Dim reader As New StreamReader(response)
        Dim res = reader.ReadToEnd()
        reader.Close()
        response.Close()

        Return res
    End Function

    Public Shared Function GenerateMd5Hash(input As String) As String
        Dim x = New System.Security.Cryptography.MD5CryptoServiceProvider()
        Dim computeHash = System.Text.Encoding.UTF8.GetBytes(input)
        computeHash = x.ComputeHash(computeHash)
        Dim stringBuilder = New System.Text.StringBuilder(computeHash.Length)
        For i As Int16 = 0 To computeHash.Length - 1
            stringBuilder.Append(computeHash(i).ToString("x2"))
        Next
        Return stringBuilder.ToString()
    End Function

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click

        'Send Init Command
        Dim data = Encoding.UTF8.GetBytes("{""app_id"" : ""123"",""app_secret"" : ""123""}")
        Dim FilmOn As New Uri("http://www.filmon.com/tv/api/init")
        Dim response As String = SendRequest(FilmOn, data, "application/json", "POST")
        Dim UserPass As String = GenerateMd5Hash(TxtPassword.Text)
        Dim P1 As Integer = 0
        Dim P2 As Integer
        Dim RecTitle As String
        Dim RecStatus As String
        Dim RecLink As String
        Dim RecDate As String
        Dim filepath As String
        Dim DelResponse As String

        Try

            'Get the Session Key
            SessionKey = Mid$(response, InStr(response, "session_key") + 14, 32)


            'Log In User
            Dim LogIn As New Uri("http://www.filmon.com/tv/api/login?session_key=" + SessionKey)
            data = Encoding.UTF8.GetBytes("{""login"" : """ + TxtUserName.Text + """,""password"" : """ + UserPass + """}")
            response = SendRequest(LogIn, data, "application/json", "POST")

            'Get recording list for user
            Dim DVRList As New Uri("http://www.filmon.com/tv/api/dvr/list?session_key=" + SessionKey)
            data = Encoding.UTF8.GetBytes("")

            response = SendRequest(DVRList, data, "application/json", "POST")

            While P1 <> -1
                ' Get the recording title
                P1 = response.IndexOf(",""title"":""", P1)
                If P1 <> -1 Then
                    RecTitle = Mid$(response, P1 + 11, InStr(Mid$(response, P1 + 12), """"))
                    ' Get the recording status
                    P1 = response.IndexOf(",""status"":""", P1)
                    RecStatus = Mid$(response, P1 + 12, InStr(Mid$(response, P1 + 13), """"))
                    RecTitle = Regex.Replace(RecTitle, "\?", "")
                    RecTitle = Regex.Replace(RecTitle, "'", "")
                    RecTitle = Regex.Replace(RecTitle, ":", "")

                    If RecStatus = "Recorded" Then
                        ' Get the recording url
                        ' P1 = response.IndexOf(",""download_link"":""", P1)
                        P1 = response.IndexOf(",""stream_url"":""", P1)
                        RecLink = Mid$(response, P1 + 19, InStr(Mid$(response, P1 + 20), """"))

                        P2 = RecLink.IndexOf(".mp4") - 18
                        ' Get recording date
                        RecDate = Mid(RecLink, P2, 10)
                        ' Format save file name
                        RecStatus = RecTitle & " " & Mid(RecDate, 9, 2) & "-" & Mid(RecDate, 6, 2) & "-" & Mid(RecDate, 3, 2) & ".mp4"
                        ' Create Linux download command: wget http:\/\/s3.dvr.gv.filmon.com\/schdld\/14\/36\/248\/2015.09.06\/2632049.mp4 -O Lady Chatterley's Lover.mp4
                        ' Text1.Text = Text1.Text & "wget " & RecLink & " -O """ & RecTitle & " " & Mid(RecDate, 9, 2) & "-" & Mid(RecDate, 6, 2) & "-" & Mid(RecDate, 3, 2) & ".mp4""" & Environment.NewLine
                        Text1.Text = Text1.Text & "wget htt" & Mid(RecLink, 1, RecLink.IndexOf("dvr\/_de")) & Mid(RecLink, RecLink.IndexOf("_definst") + 12, RecLink.IndexOf(".mp4") - RecLink.IndexOf("_definst") - 7) & " -O """ & RecTitle & " " & Mid(RecDate, 9, 2) & "-" & Mid(RecDate, 6, 2) & "-" & Mid(RecDate, 3, 2) & ".mp4""" & Environment.NewLine
                        If CheckBox1.Checked Then
                            RecLink = "htt" & Mid(RecLink, 1, RecLink.IndexOf("dvr\/_de")) & Mid(RecLink, RecLink.IndexOf("_definst") + 12, RecLink.IndexOf(".mp4") - RecLink.IndexOf("_definst") - 7)
                            RecLink = Regex.Replace(RecLink, "\\", "")
                            RecLink = "http://hls" + Mid(RecLink, 11)
                            ' Download the recording
                            filepath = System.IO.Path.Combine(My.Computer.FileSystem.SpecialDirectories.MyPictures, "FilmOn\" & RecStatus)
                            My.Computer.Network.DownloadFile(RecLink, filepath, "", "", True, 50000, True)

                            If CheckBox2.Checked Then
                                ' remove recording from DVR list
                                data = Encoding.UTF8.GetBytes("{""record_id"" : """ + Mid(RecLink, P2 + 5, 7) + """}")
                                Dim DVRRemove As New Uri("http://www.filmon.com/tv/api/dvr/remove?session_key=" + SessionKey)
                                DelResponse = SendRequest(DVRRemove, data, "application/json", "POST")
                                ' MsgBox(DelResponse)
                            End If

                        End If

                    End If

                End If

            End While

        Catch ex As Exception

            MsgBox("Error" + ex.Message)

        End Try


    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click

        My.Settings.UserName = TxtUserName.Text
        My.Settings.Password = TxtPassword.Text


        Application.Exit() 'Exit

    End Sub

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load

        TxtUserName.Text = My.Settings.UserName
        TxtPassword.Text = My.Settings.Password

    End Sub
End Class
