If you are creating a new web project, then:

- Add project reference to 'Shared' project

- Create file '_Imports.razor' and add '@using Microsoft.AspNetCore.Components.Web'

- Create a 'Services' folder and add the relevant services.

- Create folder 'wwwroot'

- Create file 'index.html' in 'wwwroot' folder with the following content:
index.html
```
-> see pFemmeExample
```

- Recommendation: Add a splash screen here under wwwroot and define it as 'SplashScreen' in the properties during the build process.

- Add project reference to 'Shared' project

- Update your 'csproj' file as shown below:

Examle pFemmeExample.Wpf.csproj:
```
-> see pFemmeExample
```

- Update your 'MainWindow.xaml' file as shown below:

Example MainWindow.xaml:
```
-> see pFemmeExample
```


- Update your 'MainWindow.xaml.cs' file as shown below:

Examle MainWindow.xaml.cs:
```
-> see pFemmeExample
```

- Update your 'App.xaml.cs' file as shown below:

Examle App.xaml.cs:
```
-> see pFemmeExample
```

- Add 'app.manifest' file as shown below:

Example app.manifest:
```
-> see pFemmeExample
```