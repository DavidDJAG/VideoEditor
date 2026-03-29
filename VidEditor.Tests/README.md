# VideoEditor.Tests

Este proyecto es una suite de pruebas xUnit.

## Como ejecutarlo en Visual Studio

- No use `F5` ni `Ctrl+F5` sobre `VideoEditor.Tests` para validar la suite.
- Abra `Test > Test Explorer`.
- Use `Run All Tests` o ejecute pruebas individuales desde el explorador.
- Si quiere iniciar la aplicacion, establezca `VideoEditor.UI` como `Set as Startup Project`.

## Como ejecutarlo por consola

```bash
dotnet test VideoEditor.Tests/VideoEditor.Tests.csproj
```

## Nota

Si Visual Studio muestra que `dotnet.exe` se cerro con codigo `0`, eso no representa un fallo de pruebas; solo indica que el proceso termino normalmente. La validacion real de este proyecto debe verse en `Test Explorer` o en la salida de `dotnet test`.
