# 建立 runtime image（比較小）
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
# Render 預設用 10000/8080 之類的 port，我們自己用 8080
EXPOSE 8080

# 建立 build image
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# 把整個 repo 複製進來
COPY . .

# 進到 API 專案資料夾（這裡請確認實際路徑）
WORKDIR "/src/THSRBooking.Api/THSRBooking.Api"

# 編譯 + publish
RUN dotnet publish -c Release -o /app/publish

# 最終 image
FROM base AS final
WORKDIR /app

# 從 build image 把 publish 出來的東西搬過來
COPY --from=build /app/publish .

# 告訴 ASP.NET Core 要聽 8080 port
ENV ASPNETCORE_URLS=http://+:8080

# 啟動你的網站（dll 名稱要跟實際專案一致）
ENTRYPOINT ["dotnet", "THSRBooking.Api.dll"]
