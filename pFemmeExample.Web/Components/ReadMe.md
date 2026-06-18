If you are creating a new web project, then:
- Delete the 'Page' and 'Layout' folders from the 'Web' project.
- Delete the file Routes.razor from the 'Web' project.
- In the file _Imports.razor, remove the 'using' reference to the layout (@using YOUR_PROJECT.Components.Layout)
- Add the following new using statement to the file '_Imports.razor': '@using Shared'
- Add the following new using statement to the file '_Imports.razor': '@using Shared.Layout'

Change (if needed) in App.razor:
- Text of: Primary SEO, Open Graph, Twitter Card 
- Set 'Shared.Global.Configuration.ConfigGeneral.ApplicationName' and 'Shared.Global...ApplicationDomain'
- Save image 'opengraphimg.jpg' in Shared/img/opengraphimg.jpg 