# Knitty Kitty

Курсовой проект по теме магазина товаров: Uno Platform + C# приложение для покупки плюшевых изделий. Исходная WinUI 3 версия сохранена, кроссплатформенная версия находится в `src/KnittyKitty.Uno`.

## Состав решения

- `src/KnittyKitty.Core` - доменная логика магазина: товары, корзина, покупатель, оплата, кешбэк, чеки.
- `src/KnittyKitty.App` - графический интерфейс WinUI 3 по MVVM.
- `src/KnittyKitty.Uno` - кроссплатформенное Uno Platform приложение для Windows, WebAssembly и Skia Desktop.
- `tests/KnittyKitty.Tests` - модульные тесты MSTest.

## Что реализовано по заданию

- ООП: абстрактный `ProductBase`, наследники `PlushToy` и `WeightedPlushMaterial`, инкапсуляция балансов покупателя.
- SOLID: UI зависит от сервисов и абстракций, бизнес-логика вынесена из окна приложения.
- Архитектурный шаблон: MVVM.
- Паттерны: Strategy для способов оплаты, Repository для SQLite-хранилища товаров, Factory для создания доменных товаров из записей БД.
- Каталог загружается из `src/KnittyKitty.App/Data/knittykitty.db`.
- Uno-версия при первом запуске копирует seed SQLite из ресурсов приложения в локальное хранилище платформы и работает уже с этой копией.
- Корзина поддерживает добавление и удаление товаров.
- Для товаров на вес требуется взвешивание перед добавлением.
- Оплата поддерживает наличные, дебетовую карту, бонусы и смешанные платежи.
- После покупки начисляется кешбэк.
- В рамках сессии ведётся история покупок.
- После каждой покупки создаётся отдельный текстовый чек в папке `Receipts` рядом с исполняемым файлом.
- Покрыты тестами корзина, частичная оплата, нехватка средств и загрузка товаров из SQLite.

## Команды

```powershell
dotnet restore KnittyKitty.sln
dotnet test --project tests\KnittyKitty.Tests\KnittyKitty.Tests.csproj
dotnet build src\KnittyKitty.Uno\KnittyKitty.Uno\KnittyKitty.Uno.csproj -f net10.0-desktop
dotnet build src\KnittyKitty.App\KnittyKitty.App.csproj -p:Platform=x64
dotnet run --project src\KnittyKitty.Uno\KnittyKitty.Uno\KnittyKitty.Uno.csproj -f net10.0-desktop
dotnet run --project src\KnittyKitty.App\KnittyKitty.App.csproj -p:Platform=x64
dotnet workload restore src\KnittyKitty.Uno\KnittyKitty.Uno\KnittyKitty.Uno.csproj
dotnet run --project src\KnittyKitty.Uno\KnittyKitty.Uno\KnittyKitty.Uno.csproj -f net10.0-browserwasm
```

## WebAssembly в Visual Studio

Если запуск профиля `KnittyKitty.Uno (WebAssembly)` завершается ошибкой `wasm-tools workload could not be located`, установите компонент **.NET WebAssembly build tools** через Visual Studio Installer или выполните из PowerShell от имени администратора:

```powershell
dotnet workload install wasm-tools
```

После установки перезапустите Visual Studio и выберите профиль `KnittyKitty.Uno (WebAssembly)`.

## Сценарии для демонстрации

1. Выбрать `Плюшевая пряжа`, попробовать добавить без взвешивания - приложение покажет предупреждение.
2. Взвесить пряжу, добавить в корзину, добавить обычную игрушку.
3. Нажать `Смешанная`, затем `Купить и сохранить чек` - покупка пройдет частями и появится запись в истории.
4. Положить товаров больше доступных средств - появится сообщение о нехватке денег, после чего можно выложить товар.
