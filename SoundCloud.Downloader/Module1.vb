Imports Newtonsoft.Json
Imports System.Xml
Imports System.IO



Module Module1



    Private Const MUSIC_DIRECTORY As String = "C:\Users\Slyman\Music"



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

            Console.WriteLine("Finished")

        End If




    End Sub



    Private Sub Authorise()


        Dim authorised As Boolean = False


        Dim clientID As String = "e27237feeacad4b742df8473e676cb97"
        Dim clientSecret As String = "86a3e43ddbace10e5c9c5427aa4266a2"

        Dim username As String = "superslythe"
        Dim password As String = "b26a8c7d7d"

        Dim postData As String = _
            "client_id=" & clientID & _
            "&client_secret=" & clientSecret & _
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

            'Auth failed
            _authorisation = Nothing

        End Try



    End Sub



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

                If canDownload.ToLower = "true" Then

                    downloadableTrack = New TrackModel(track("id").InnerText, _
                                                       track("title").InnerText)

                    downloadableTrack.downloadUri = track("download-url").InnerText


                    Dim userNode As XmlNode = track.SelectSingleNode("user")

                    user = New UserModel

                    With user
                        .id = userNode("id").InnerText
                        .username = userNode("username").InnerText
                    End With

                    downloadableTrack.owner = user


                    lstDownloadableTracks.Add(downloadableTrack)


                End If


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


                _webClient.DownloadFile(trackInfo.downloadUri & _
                                        "?oauth_token=" & _authorisation.access_token, _
                                        downloadedSong.FullName)

                Console.WriteLine("     track downloaded.")


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
