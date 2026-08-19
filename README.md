# FocusPomodoro

Timer Pomodoro compacto para Windows, feito com WinUI 3 e Windows App SDK. A janela fica no canto da tela, o ciclo de foco/pausas roda de forma precisa e o app pode continuar na bandeja do sistema.

## Pré-requisitos

- Windows 10 1809 (build 17763) ou superior
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) 17.14+ **ou** o workload **Windows application development**
  - Windows App SDK
  - Windows 10/11 SDK (10.0.19041 ou mais recente)
- Arquitetura de build: `x64` (também há `x86` e `ARM64`)

## Restaurar pacotes

Na raiz do repositório:

```powershell
dotnet restore FocusPomodoro\FocusPomodoro.csproj
dotnet restore FocusPomodoro.Tests\FocusPomodoro.Tests.csproj
```

No Visual Studio, abrir `FocusPomodoro\FocusPomodoro.csproj` (ou a pasta do repositório) e aguardar a restauração automática do NuGet.

## Compilar

```powershell
dotnet build FocusPomodoro\FocusPomodoro.csproj -c Debug -p:Platform=x64
dotnet test FocusPomodoro.Tests\FocusPomodoro.Tests.csproj
```

No Visual Studio: selecione a plataforma **x64** e use **Compilar → Compilar Solução** (ou o projeto `FocusPomodoro`).

## Executar

Pela linha de comando:

```powershell
dotnet run --project FocusPomodoro\FocusPomodoro.csproj -c Debug -p:Platform=x64
```

No Visual Studio:

1. Abra `FocusPomodoro\FocusPomodoro.csproj`.
2. Defina `FocusPomodoro` como projeto de inicialização.
3. Escolha a plataforma **x64**.
4. Pressione **F5** (depuração) ou **Ctrl+F5** (sem depuração).

O projeto é empacotado como MSIX (`WindowsPackageType=MSIX`). Na primeira execução pelo Visual Studio, o app é implantado no perfil do usuário.

## Configurações locais

As preferências ficam em `settings.json`, na pasta de dados locais do pacote:

```text
%LOCALAPPDATA%\Packages\FocusPomodoro_*\LocalState\settings.json
```

O arquivo é criado na primeira execução, carregado **antes** de o timer e a janela serem configurados, e salvo ao encerrar o app (incluindo **Sair completamente** no menu da bandeja).

## Publicar como MSIX

Pela linha de comando:

```powershell
dotnet publish FocusPomodoro\FocusPomodoro.csproj -c Release -p:Platform=x64
```

O pacote é gerado em:

```text
FocusPomodoro\bin\x64\Release\net10.0-windows10.0.19041.0\win-x64\AppPackages\
```

No Visual Studio:

1. Clique com o botão direito em `FocusPomodoro`.
2. **Package and Publish → Create App Packages…**
3. Escolha **Sideloading** (ou a Store, se for o caso) e conclua o assistente.

Instale o `.msix` / `.msixbundle` gerado. Em sideloading, o certificado de desenvolvimento precisa ser confiável na máquina.

## Recursos implementados

- Ciclo Pomodoro: foco, pausa curta e pausa longa (4 focos → pausa longa)
- Timer baseado em horário de término, preciso após pausa, suspensão ou travamento breve da UI
- Início, pausa, retomada, reinício da fase e pular fase
- Configurações persistentes: tempos, ciclos, tema, sempre visível, sons, notificações e minimizar para a bandeja
- Janela compacta, sempre no topo opcional e posição/tamanho lembrados
- Ícone na bandeja, com controle do timer e **Sair completamente**
- Toasts do Windows ao encerrar cada fase (respeitam `NotificationsEnabled`)
- Som curto de troca de fase (respeita `SoundEnabled`, sem bloquear a UI)
- Autoinício da próxima fase configurável
