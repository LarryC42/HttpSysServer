Imports System.Net
Imports System.Dynamic

''' <summary>
''' Interface for handling an http.sys request
''' </summary>
Public Interface IHandleRequest
	''' <summary>
	''' Description of handler
	''' </summary>
	''' <returns></returns>
	Property Description As String

	''' <summary>
	''' Asynchronous handler for request
	''' </summary>
	''' <param name="context">Request context</param>
	''' <param name="segments">URL segments</param>
	''' <param name="urlParameters">URL parameters, _command has non-parameter URL segments</param>
	''' <returns></returns>
	Function HandleAsync(context As HttpListenerContext, segments() As String, urlParameters As Dictionary(Of String, List(Of String))) As Task(Of Boolean)
End Interface