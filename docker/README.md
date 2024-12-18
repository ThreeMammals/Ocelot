# docker build

This folder contains the `Dockerfile.*` and `build-*.sh` scripts to create the Ocelot build image & container.

## Account
- Docker Hub | [Ocelot Gateway](https://hub.docker.com/u/ocelot2)

## Repositories
- [circleci-build](https://hub.docker.com/r/ocelot2/circleci-build)

## Outdated Tags
- [8.21.0](https://hub.docker.com/layers/ocelot2/circleci-build/8.21.0/images/sha256-edb46d37ab52d39a5b27dc63895e5944d4d491d1788744ed144ecb4303b94532?context=explore), uploaded on Nov 20, 2023. It contains .NET 8, 7 and 6 SDKs. It supports builds for `net6.0`, `net7.0` and `net8.0` frameworks.
- [8.23.2](https://hub.docker.com/layers/ocelot2/circleci-build/8.23.2/images/sha256-981d6f9e6e5ba54f6e044bca6fcf8b5197a8f3e6ce2b3cdfa9e6704ecd2ca969?context=explore), uploaded on Apr 3, 2024. It supports development SSL certificates by the command `dotnet dev-certs https`.
- [latest](https://hub.docker.com/layers/ocelot2/circleci-build/latest/images/sha256-981d6f9e6e5ba54f6e044bca6fcf8b5197a8f3e6ce2b3cdfa9e6704ecd2ca969?context=explore) is version 8.23.2, uploaded on Apr 3, 2024.

## .NET 8-9 Tags

### Single SDK Tags
- [sdk-8-alpine-lin.net8](https://hub.docker.com/layers/ocelot2/circleci-build/sdk-8-alpine-lin.net8/images/sha256-17c21438771641ba2a3320a6c13fe756a851d707a925188d9616485bfc757b22?context=explore), uploaded on Dec 3, 2024. This Linux OS image contains .NET 8 SDK only. It supports builds for `net8.0` framework.
- [sdk-9-alpine-lin.net9](https://hub.docker.com/layers/ocelot2/circleci-build/sdk-9-alpine-lin.net9/images/sha256-c20d82a52c1a7eebf86bcd32751960ae189e6c50575959ee0499b1d88541c9ea?context=explore), uploaded on Dec 3, 2024. This Linux OS image contains .NET 9 SDK only. It supports builds for `net9.0` framework.
- [sdk8-nanoserver2022-win.net8](https://hub.docker.com/layers/ocelot2/circleci-build/sdk8-nanoserver2022-win.net8/images/sha256-20f21f4361d18301bc885e1f01ebefcd6b024802130db1296173cdfaac5d75e6?context=explore), uploaded on Dec 3, 2024. This Windows OS image contains .NET 8 SDK only. It supports builds for `net8.0` framework.
- [sdk9-nano2022-win.net9](https://hub.docker.com/layers/ocelot2/circleci-build/sdk9-nano2022-win.net9/images/sha256-37b718885f8cfb3480299f48044836f01aec2370e331667dd1811a4c94a4ce45?context=explore), uploaded on Dec 3, 2024. This Windows OS image contains .NET 9 SDK only. It supports builds for `net9.0` framework.

### Double SDKs Tags
- [sdk9-alpine-lin.net8-9](https://hub.docker.com/layers/ocelot2/circleci-build/sdk9-alpine-lin.net8-9/images/sha256-707924d144248178caff80578649f7d0e9b7c70ed51b6fb5171170c9a30a4eae?context=explore) aka [9.24.0](https://hub.docker.com/layers/ocelot2/circleci-build/9.24.0/images/sha256-707924d144248178caff80578649f7d0e9b7c70ed51b6fb5171170c9a30a4eae?context=explore), uploaded on Dec 3, 2023. This Linux OS image contains .NET 8 and 9 SDKs. It supports builds for `net8.0` and `net9.0` frameworks.
- [sdk9-nano2022-win.net8-9](https://hub.docker.com/layers/ocelot2/circleci-build/sdk9-nano2022-win.net8-9/images/sha256-6e44a8fc52ab091ea43090b6ab182f7a05d7ac31402ce742a7e035323e584cf7?context=explore) aka [9.24.win](https://hub.docker.com/layers/ocelot2/circleci-build/9.24.win/images/sha256-6e44a8fc52ab091ea43090b6ab182f7a05d7ac31402ce742a7e035323e584cf7?context=explore), uploaded on Dec 4, 2023. This Windows OS image contains .NET 8 and 9 SDKs. It supports builds for `net8.0` and `net9.0` frameworks.

### Links
- Docker Hub | [Microsoft dotnet Images](https://hub.docker.com/r/microsoft/dotnet)
- GitHub | dotnet-docker | [.NET SDK](https://github.com/dotnet/dotnet-docker/blob/main/README.sdk.md)
- StackOverflow | [PowerShell and process exit codes](https://stackoverflow.com/questions/57468522/powershell-and-process-exit-codes)
