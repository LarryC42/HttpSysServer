Imports System.Net
Imports HttpServer
Public Class FileServer
	Implements IHandleRequest

	Public Property Description As String Implements IHandleRequest.Description
		Get
			Return "File server"
		End Get
		Set(value As String)
		End Set
	End Property

	Public Function HandleAsync(context As HttpListenerContext, segments() As String, urlParameters As Dictionary(Of String, List(Of String))) As Task(Of Boolean) Implements IHandleRequest.HandleAsync
		Dim filename = segments
	End Function
End Class
