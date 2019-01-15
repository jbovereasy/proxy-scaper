Imports System.IO
Imports System.Net
Imports System.Text
Imports System.Text.RegularExpressions

Module Module1
    Const quote As String = """"

    Sub Main()

        Console.WriteLine("Press any key to start...")
        Console.ReadKey()
        Console.WriteLine("Process has started...")

        Dim values As List(Of Long) = New List(Of Long)
        scrapeProxies(values)

        Console.WriteLine("Done Scraping...")
        Console.WriteLine("Press any key to upload the scraped proxy list...")
        Console.ReadKey()

        uploadProxies(values)

        Console.WriteLine("Done Uploading...")
        Console.WriteLine("Press any key to close the program")
    End Sub

    Private Sub scrapeProxies(values As List(Of Long))
        Try
            Parallel.ForEach(File.ReadLines(Directory.GetCurrentDirectory() & "\proxysources.txt"),' New ParallelOptions With {.MaxDegreeOfParallelism = 10},
                             Sub(line)
                                 Dim value As String = New System.Net.WebClient().DownloadString(line)
                                 Dim matches As MatchCollection = Regex.Matches(value, "(\d{1,3}\.){3}\d{1,3}") '':(\d+)"

                                 For Each m As Match In matches
                                     For Each c As Capture In m.Captures
                                         values.Add(IPAddressToLong(IPAddress.Parse(c.Value)))
                                         'Console.WriteLine(c.Value & " " & line)
                                     Next
                                 Next

                             End Sub)
        Catch ex As Exception
            Console.WriteLine(ex)
        End Try
    End Sub

    Private Sub uploadProxies(values As List(Of Long))
        Dim ProxyAPI_Uri As New Uri("http://proxyapi.tech/php-crud-api-2.0.4/api.php/records/proxyapi/")

        For Each element In values
            Try
                Dim postData = Encoding.UTF8.GetBytes("{ " & quote & "ipv4" & quote & ": " & quote & element & quote & ", " & quote & "date_added" & quote & ": " & quote & DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") & quote & " }")
                Dim result_post = SendJsonPostRequest(ProxyAPI_Uri, postData)
                Console.WriteLine("{ " & quote & "ipv4" & quote & ": " & quote & element & quote & ", " & quote & "date_added" & quote & ": " & quote & DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") & quote & " }")
            Catch webExcp As WebException
                ' If you reach this point, an exception has been caught.
                Console.WriteLine("A WebException has been caught.")
                ' Write out the WebException message.
                'Console.WriteLine(webExcp.ToString())
                ' Get the WebException status code.
                Dim status As WebExceptionStatus = webExcp.Status
                ' If status is WebExceptionStatus.ProtocolError, there has been a protocol error and a WebResponse should exist.
                ' Display the protocol error.
                If status = WebExceptionStatus.ProtocolError Then
                    Console.Write("The server returned protocol error ")
                    ' Get HttpWebResponse so that you can check the HTTP status code.
                    Dim httpResponse As HttpWebResponse =
                       CType(webExcp.Response, HttpWebResponse)
                    Console.WriteLine(CInt(httpResponse.StatusCode).ToString() &
                       " - " & httpResponse.StatusCode.ToString())
                End If
            Catch ex As Exception
                ' Code to catch other exceptions
                Console.WriteLine(ex)
            End Try
        Next
    End Sub

    Public Function IPAddressToLong(address As System.Net.IPAddress) As Long
        Dim byteIP As Byte() = address.GetAddressBytes()

        Dim ip As Long = CLng(byteIP(3)) << 24
        ip += CLng(byteIP(2)) << 16
        ip += CLng(byteIP(1)) << 8
        ip += CLng(byteIP(0))
        Return ip
    End Function

    Public Function SendJsonPostRequest(uri As Uri, jsonDataBytes As Byte()) As String
        Dim req As WebRequest = WebRequest.Create(uri)
        req.ContentType = "application/json"
        req.Method = "POST"
        req.ContentLength = jsonDataBytes.Length

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

End Module