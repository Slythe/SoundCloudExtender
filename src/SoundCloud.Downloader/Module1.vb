Imports Newtonsoft.Json
Imports System.Xml
Imports System.IO



Module Module1



    Private Const MUSIC_DIRECTORY As String = "E:\Users\Slyman\My Music\"



#Region "Private Properties"



    Private Property _webClient As Net.WebClient


    Private Property _authorisation As TokenInfoModel



#End Region



#Region "Constructors"



    Private Sub setUp()

        Console.WriteLine("SoundCloud Downloader Started")

        _webClient = New Net.WebClient


    End Sub



#End Region



    Sub Main()


        setUp()


        Authorise()


        If canAccessAPI() Then


            Dim followingUsrs As List(Of String) = getFollowing()


            For Each user As String In followingUsrs

                Console.WriteLine("Looking for tracks to download from user " & user)

                downloadTracksForUser(user)

            Next

            Console.WriteLine("Finished!")

        End If




    End Sub



    Private Sub Authorise()


        Dim authorised As Boolean = False


        Console.WriteLine("Please enter your SoundCloud login details")

        Dim username As String = getUsername()
        Dim password As String = "b26a8c7d7d"



        Console.Write("Password: ")

        password = Console.ReadLine


        Do Until Not String.IsNullOrWhiteSpace(password)

            Console.WriteLine("No password detected, please try again")
            Console.Write("Password: ")

            password = Console.ReadLine

        Loop



        Dim postData As String = _
            "client_id=" & My.Settings.clientID & _
            "&client_secret=" & My.Settings.clientSecret & _
            "&grant_type=password" & _
            "&username=" & username & _
            "&password=" & password



        Dim tokenInfo As String = String.Empty
        Dim tokenUri As String = "https://api.soundcloud.com/oauth2/token"



        Try

            tokenInfo = _webClient.UploadString(tokenUri, postData)

            _authorisation = _
                JsonConvert.DeserializeObject(Of TokenInfoModel)(tokenInfo)

            Console.WriteLine("User " & username & " authorised")

        Catch ex As Net.WebException

            Dim WebResponse As System.Net.HttpWebResponse = ex.Response

            If WebResponse.StatusCode = Net.HttpStatusCode.Unauthorized Then

                'Auth failed
                _authorisation = Nothing

            Else

                'Failed for some other reason - log

                _authorisation = Nothing

            End If


        End Try



    End Sub


    Private Function getUsername() As String


        Dim username As String = String.Empty


        Console.Write("Username: ")

        username = Console.ReadLine

        Dim attemptCount As Integer = 1

        Do Until Not String.IsNullOrWhiteSpace(username)

            Console.WriteLine("No username detected, please try again (attempt " & attemptCount.ToString & " of 3)")
            Console.Write("Username: ")

            username = Console.ReadLine

            'no more than 3 attempts
            If attemptCount = 3 Then

                Exit Do

            Else

                attemptCount += 1

            End If

        Loop


        Return username


    End Function


    Private Function canAccessAPI() As Boolean


        Dim canAccess As Boolean = False


        Dim soundCloudMeRes As String = _
            "https://api.soundcloud.com/me.xml"


        Dim apiCallResult As String = String.Empty


        Try


            apiCallResult = _
                _webClient.DownloadString(soundCloudMeRes & "?oauth_token=" + _authorisation.access_token)


            canAccess = True

            Console.WriteLine("User has API access")

        Catch ex As Exception

            'can't access

        End Try


        Return canAccess


    End Function



    Private Function getFollowing() As List(Of String)


        Console.WriteLine("Getting the users followed")


        Dim soundCloudMeRes As String = _
            "https://api.soundcloud.com/me/followings.xml"


        Dim apiCallResult As String = String.Empty

        Dim lstFollowing As New List(Of String)


        Try


            apiCallResult = _
                _webClient.DownloadString(soundCloudMeRes & "?oauth_token=" + _authorisation.access_token)


            Dim xDoc As New Xml.XmlDocument
            xDoc.LoadXml(apiCallResult)


            Dim userNodes As Xml.XmlNodeList = xDoc.SelectNodes("/users/user")

            For Each user As XmlNode In userNodes

                lstFollowing.Add(user("id").InnerText)

                Console.WriteLine(" following user " & user("username").InnerText)

            Next


        Catch ex As Exception

            'can't access

        End Try


        Return lstFollowing


    End Function



    Private Function getDownloadableTracksForUser(ByVal userID As String) As List(Of TrackModel)


        Dim soundCloudMeRes As String = _
           "https://api.soundcloud.com/users/" & userID & "/tracks.xml"


        Dim apiCallResult As String = String.Empty

        Dim lstDownloadableTracks As New List(Of TrackModel)


        Try


            apiCallResult = _
                _webClient.DownloadString(soundCloudMeRes & "?oauth_token=" + _authorisation.access_token)


            Dim xDoc As New Xml.XmlDocument
            xDoc.LoadXml(apiCallResult)


            Dim canDownload As String = String.Empty
            Dim downloadableTrack As TrackModel = Nothing
            Dim user As UserModel = Nothing

            Dim tracksNodes As Xml.XmlNodeList = xDoc.SelectNodes("/tracks/track")

            For Each track As XmlNode In tracksNodes

                canDownload = track("downloadable").InnerText

                downloadableTrack = New TrackModel(track("id").InnerText, _
                                       track("title").InnerText)


                If canDownload.ToLower = "true" Then

                    downloadableTrack.downloadUri = track("download-url").InnerText

                Else

                    downloadableTrack.downloadUri = track("stream-url").InnerText

                End If







                Dim userNode As XmlNode = track.SelectSingleNode("user")

                user = New UserModel

                With user
                    .id = userNode("id").InnerText
                    .username = userNode("username").InnerText
                End With

                downloadableTrack.owner = user


                lstDownloadableTracks.Add(downloadableTrack)


            Next


        Catch ex As Exception

            'can't access

        End Try


        Return lstDownloadableTracks


    End Function



    Private Sub downloadTracksForUser(ByVal userID As String)


        Dim lstDownloadableTracks As List(Of TrackModel) = _
            getDownloadableTracksForUser(userID)


        Dim downloadedSong As FileInfo = Nothing


        For Each trackInfo As TrackModel In lstDownloadableTracks


            downloadedSong = _
                New FileInfo(Path.Combine(MUSIC_DIRECTORY, _
                                          "SoundCloud\" & _
                                          trackInfo.owner.username & "\" & _
                                          makeFileNameSafe(trackInfo.title) & _
                                          ".mp3"))

            If Not downloadedSong.Exists Then


                Console.WriteLine(" downloading track " & trackInfo.title & " from user " & trackInfo.owner.username)

                createDirectoryIfRequired(trackInfo.owner)

                Try

                    _webClient.DownloadFile(trackInfo.downloadUri & _
                                            "?oauth_token=" & _authorisation.access_token, _
                                            downloadedSong.FullName)


                    Console.WriteLine("     track downloaded.")


                Catch ex As Exception

                    Console.WriteLine("     download failed: " & ex.Message)

                End Try


            Else

                Console.WriteLine("     already in collection. Not downloaded.")

            End If


        Next



    End Sub



    Private Sub createDirectoryIfRequired(ByVal user As UserModel)


        Dim directoryPath As String = _
            Path.Combine(MUSIC_DIRECTORY, "SoundCloud\" & user.username)

        Dim saveDirectory As DirectoryInfo = _
            New DirectoryInfo(directoryPath)


        If Not saveDirectory.Exists Then

            saveDirectory.Create()

        End If


    End Sub



    Private Function makeFileNameSafe(ByVal fileName As String) As String


        'TODO: Swap out with regex
        Dim safeFileName As String = _
            fileName.Replace("\", "-").Replace("/", "-").Replace(":", "-").Replace("?", "-").Replace("-""-", "-").Replace("<", "-").Replace(">", "-").Replace("|", "-")


        Return safeFileName


    End Function



End Module
