Gotchas
=============
	
**Note:** When using ASP.NET Core 2.2 and you want to use In-Process hosting, replace **.UseIISIntegration()** with **.UseIIS()**, otherwise you'll get startup errors.