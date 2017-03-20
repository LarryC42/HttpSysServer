Imports System.Net
Imports System.Threading.Tasks
Imports System.Dynamic
Imports System.Text

''' <summary>
''' General http server.
''' 
''' To reserve URL use something like the following:
''' netsh http add urlacl url=http://+:80/api user=Everyone
''' http://+:80/api should be a combination of site and rootPath.
''' Everyone should be the group allowed to access the site.
''' </summary>
Public Class HttpServer
	Implements IDisposable
#Region " Private variables "
	Private _isAsync As Boolean = True
	Private _listener As HttpListener = Nothing
	Private _isRunning As Boolean = False
	Private _isPaused As Boolean = False
	Private _requestsInQueue As Integer = 0
	Private _site As String = "http://+:80/"
	Private _rootPath As String = "svc/"
#End Region
#Region " Properties "
	''' <summary>
	''' Max number of in process requests before server returns a busy error
	''' </summary>
	''' <returns></returns>
	Public Property MaxQueuedRequests As Integer = 1000
	''' <summary>
	''' Return the site
	''' </summary>
	''' <returns>String such as http://+:80/</returns>
	Public Property Site As String
		Get
			Return _site
		End Get
		Set(value As String)
			If value = String.Empty OrElse (String.Compare(Left(value, 7), "http://", True) <> 0 AndAlso String.Compare(Left(value, 8), "https://", True) <> 0) Then
				Throw New ArgumentException("Invalid protocol should be http://... or https://...")
			End If
			If Right(value, 1) <> "/" Then
				_site = String.Format("{0}/", value)
			Else
				_site = value
			End If
		End Set
	End Property
	''' <summary>
	''' Return the root path under the site
	''' </summary>
	''' <returns>String for example svc/</returns>
	Public Property RootPath As String
		Get
			Return _rootPath
		End Get
		Set(value As String)
			If value <> String.Empty AndAlso Right(value, 1) <> "/" Then
				_rootPath = String.Format("{0}/", value)
			Else
				_rootPath = value
			End If
		End Set
	End Property
	Public ReadOnly Property IsRunning As Boolean
		Get
			Return (_isRunning AndAlso _listener IsNot Nothing)
		End Get
	End Property
	''' <summary>
	''' Returns the authorization routine for the site
	''' </summary>
	''' <returns>IProcessContext</returns>
	Public Property AuthorizeRequest As IHandleRequest = Nothing
	''' <summary>
	''' Routine that handles a request
	''' </summary>
	''' <returns>IProcessContext</returns>
	Public Property HandleRequest As IHandleRequest = Nothing
	''' <summary>
	''' Authentication scheme the site uses
	''' </summary>
	''' <returns>AuthenticationSchemes</returns>
	Public Property AuthenticationScheme As AuthenticationSchemes = AuthenticationSchemes.Anonymous
#End Region

	''' <summary>
	''' Configure the http server
	''' </summary>
	''' <param name="site">String: Site such as http://+:80/</param>
	''' <param name="rootPath">Root path under site such as svc/ or api/</param>
	''' <param name="authentication">AuthenticationSchemes: method of authentication</param>
	''' <param name="handleRequests">Request handler</param>
	''' <param name="authorizeRequest">Authorization handler</param>
	Public Sub Configure(Optional site As String = "http://+:80/", Optional rootPath As String = "svc/", Optional authentication As AuthenticationSchemes = AuthenticationSchemes.Anonymous, Optional handleRequests As IHandleRequest = Nothing, Optional authorizeRequest As IHandleRequest = Nothing)
		Me.Site = site
		Me.RootPath = rootPath
		Me.AuthenticationScheme = authentication
		If handleRequests IsNot Nothing Then Me.HandleRequest = handleRequests
		If authorizeRequest IsNot Nothing Then Me.AuthorizeRequest = authorizeRequest
	End Sub

	Public ReadOnly Property IsPaused As Boolean
		Get
			Return _isPaused
		End Get
	End Property
	''' <summary>
	''' Pause a running http server
	''' </summary>
	Public Sub Pause()
		If Not (_isRunning AndAlso _listener IsNot Nothing) Then Return
		_listener.Stop()
		_isPaused = True
	End Sub
	''' <summary>
	''' Resume a paused http server
	''' </summary>
	Public Sub [Resume]()
		If Not (_isRunning AndAlso _listener IsNot Nothing) Then Return
		Call BeginListening()
	End Sub
	Private Sub BeginListening()
		_isRunning = True
		_isPaused = False
		_listener.Start()
		If _isAsync Then
			Call ListenAsync()
			Call ListenAsync()
			Call ListenAsync()
			Call ListenAsync()
		Else
			_listener.BeginGetContext(New AsyncCallback(AddressOf Listen), Nothing) ' Start listening for next request
			_listener.BeginGetContext(New AsyncCallback(AddressOf Listen), Nothing) ' Start listening for next request
			_listener.BeginGetContext(New AsyncCallback(AddressOf Listen), Nothing) ' Start listening for next request
			_listener.BeginGetContext(New AsyncCallback(AddressOf Listen), Nothing) ' Start listening for next request
		End If
	End Sub

	''' <summary>
	''' Shut down http server
	''' </summary>
	Public Sub Shutdown()
		_isRunning = False
		_isPaused = False
		If _listener IsNot Nothing Then
			_listener.Close()
			_listener = Nothing
		End If
	End Sub

	''' <summary>
	''' Start the http server running
	''' </summary>
	Public Sub Startup()
		If _isRunning Then Return
		If _site = String.Empty Then
			Throw New ConstraintException("Site is required")
		End If
		_listener = New HttpListener
		Dim prefix = String.Format("{0}{1}", _site, _rootPath)
		_listener.Prefixes.Add(prefix)
		_listener.AuthenticationSchemes = Me._AuthenticationScheme
		Call BeginListening()
	End Sub
	Async Sub ListenAsync()
		While _isRunning AndAlso _listener.IsListening
			Try
				Dim context = Await _listener.GetContextAsync()
				If Not _isRunning OrElse Not _listener.IsListening Then Return
				Call RequestHandler(context)
			Catch ex As Exception
				If Not _isRunning OrElse Not _listener.IsListening Then Return
			End Try
		End While
	End Sub
	Async Function Listen(ByVal result As IAsyncResult) As Task
		If Not (_isRunning AndAlso _listener IsNot Nothing AndAlso _listener.IsListening) Then Return
		Dim context As HttpListenerContext = Nothing
		Try
			context = _listener.EndGetContext(result) ' Get context
		Catch ex As Exception
			Return
		End Try

		_listener.BeginGetContext(New AsyncCallback(AddressOf Listen), Nothing) ' Start listening for next request

		If _requestsInQueue >= MaxQueuedRequests Then
			Dim errorOccurred = False
			Try
				Await mStreamHelper.ServiceUnavailable(context)
			Catch ex As Exception
				errorOccurred = True
			End Try
			If errorOccurred Then
				Try
					context.Response.Abort()
				Catch ex As Exception
				End Try
			End If
			Return
		End If
		Dim hadError As Boolean = False
		Try
			Await RequestHandler(context)
		Catch ex As Exception
			hadError = True
		End Try
		If hadError Then
			Try
				context.Response.Abort()
			Catch ex As Exception
			End Try
		End If
	End Function
#Region " Handle requests "

	''' <summary>
	''' Handle an incoming request
	''' </summary>
	''' <param name="context">Request context</param>
	''' <returns></returns>
	Private Async Function RequestHandler(context As HttpListenerContext) As Task
		_requestsInQueue += 1
		If _requestsInQueue > MaxQueuedRequests Then
			_requestsInQueue -= 1
			Dim hadError = False
			Try
				Await mStreamHelper.ServiceUnavailable(context)
			Catch ex As Exception
				hadError = True
			End Try
			If hadError Then
				Try
					context.Response.Abort()
				Catch ex As Exception
				End Try
			End If
			Return
		End If
		If context Is Nothing Then
			_requestsInQueue -= 1
			Return  ' Nothing to handle
		End If
		Try
			' Get URL segments after rootPath and up to ? in url
			Dim segments() = mUrlHelper.GetUrlSegments(context.Request.RawUrl, _rootPath)

			' Add url parameters (after ? in url)
			Dim parameters As New Dictionary(Of String, List(Of String))(StringComparer.CurrentCultureIgnoreCase)
			Dim params = mUrlHelper.GetUrlParameters(context.Request.RawUrl)
			For Each param In params.Keys
				' Values could be in the URL and in the parameteters.  Ex: http://localhost/test/{filename} gets passed as http://locahost/test/one.csv?fileName=two.csv
				If Not parameters.ContainsKey(param) Then parameters(param) = New List(Of String)
				parameters(param).AddRange(params(param))
			Next

			If Not Await DoAuthorizeRequest(context, segments, parameters) Then
				_requestsInQueue -= 1
				Return
			End If
			Await DoHandleRequest(context, segments, parameters)
		Catch ex As Exception
		End Try
		_requestsInQueue -= 1
	End Function

	''' <summary>
	''' Validate request is authorized
	''' </summary>
	''' <param name="context"></param>
	''' <returns></returns>
	Private Async Function DoAuthorizeRequest(context As HttpListenerContext, segments() As String, parameters As Dictionary(Of String, List(Of String))) As Task(Of Boolean)
		Dim isAuthorized = True
		If _AuthorizeRequest Is Nothing Then Return True ' Valid if not authorization routine
		Try
			isAuthorized = Await _AuthorizeRequest.HandleAsync(context, segments, parameters)
		Catch ex As Exception
			isAuthorized = False
		End Try
		If isAuthorized Then Return True
		Await mStreamHelper.Unathorized(context)
		Return False
	End Function

	''' <summary>
	''' Handle request
	''' </summary>
	''' <param name="context">HttpListenerContext of request</param>
	''' <returns>True if handled, False if not handled</returns>
	Private Async Function DoHandleRequest(context As HttpListenerContext, segments() As String, parameters As Dictionary(Of String, List(Of String))) As Task(Of Boolean)
		If _HandleRequest Is Nothing Then Return False
		Dim requestHandled = True
		Try
			requestHandled = Await _HandleRequest.HandleAsync(context, segments, parameters)
		Catch ex As Exception
			requestHandled = False
		End Try
		If requestHandled Then Return True
		Await mStreamHelper.BadRequest(context)
		Return False
	End Function
#End Region

#Region "IDisposable Support"
	Private disposedValue As Boolean ' To detect redundant calls
	Protected Overridable Sub Dispose(disposing As Boolean)
		If Not disposedValue Then
			If disposing Then
				Me.Shutdown()
			End If
		End If
		disposedValue = True
	End Sub
	Public Sub Dispose() Implements IDisposable.Dispose
		Dispose(True)
	End Sub
#End Region
End Class