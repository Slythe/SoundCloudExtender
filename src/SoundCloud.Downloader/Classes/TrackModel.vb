Public Class TrackModel


    Public Property id As String

    Public Property title As String

    Public Property downloadUri As String

    Public Property owner As UserModel


    Public Sub New(ByVal id As String, _
                   ByVal title As String)

        Me.id = id
        Me.title = title


    End Sub


End Class
