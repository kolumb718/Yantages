# Техническая база

## Репозиторий
Локальная папка: W:\Planets\Yantages.
Origin: git@github.com:kolumb718/Yantages.git.
Основная ветка на момент фиксации: main. Секретные ключи не хранить в репозитории.

| Путь | Назначение |
| --- | --- |
| unity/ | Новая основная версия |
| unity/Assets/Scripts/CourierShip.cs | Генерация интерьера, движение, лифт, заказ и IMGUI |
| unity/Assets/Scripts/CourierCharacters.cs | Модели персонажей и следование совы |
| unity/Assets/Scripts/ShipCameraFraming.cs | Кадрирование дома/ангара, camera-window слежение (C — обзор), переход по высоте пассажира и видимость этажей |
| unity/Assets/Scripts/ShipFurnishings.cs | Размещение мебели и растений из prefab |
| unity/Assets/Scripts/ShipWindowView.cs | Городская сценография и параллакс по положению камеры |
| unity/Assets/Shaders/WindowCity.shader | Обрезка движущихся слоёв города по оконному проёму |
| unity/Assets/Scripts/ShipInteriorDetails.cs | Надписи, кабели и вращающаяся голограмма |
| unity/Assets/Scripts/ShipWardrobe.cs | Открытый гардероб со скафандром и снаряжением |
| unity/Assets/Scripts/ShipLift.cs | Движение платформы, пассажир, вызов и ограничения шахты |
| unity/Assets/Scripts/GlassLiftVisuals.cs | Стеклянная шахта, настил, направляющие, двери и индикаторы |
| unity/Assets/Scripts/ShipHangar.cs | Подземный ангар и процедурный зелёный корабль |
| unity/Assets/Shaders/Hologram.shader | Прозрачная сканирующая оболочка планеты |
| unity/Assets/Art/Furniture/ | FBX, палитры, материалы и prefab мебели/растений |
| unity/Assets/Editor/FurnitureSetup.cs | Подготовка мебели и привязка в Ship |
| unity/Assets/Art/Characters/ | FBX, палитры, материалы и prefab героини/совы |
| unity/Assets/Editor/CharacterSetup.cs | Подготовка персонажей и привязка в Ship |
| unity/Assets/Editor/CharacterIntegrationCheck.cs | Batch-проверка Play Mode и рендеров |
| unity/Assets/Editor/ProjectSetup.cs | Создание сцены при первом импорте |
| unity/Packages/manifest.json | Минимальные зависимости модулей |
| godot/ | Предыдущий Godot-прототип |
| src/, index.html, package.json | Первый Three.js/Vite-прототип |
| docs/ | Проектная документация |
| .agents/skills/ | Проектные навыки агента |

## Unity
Редактор проекта — Unity 6000.6.7f1 (b22767f04f04); версия получена из настоящего редактора и закреплена в ProjectVersion.txt.
Редактор установлен в C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Unity.exe.
Импорт и Play Mode подтверждены через MCP; конфигурация player-сборки ещё не проверена.
Используются Built-in renderer, Standard shader, IMGUI и старый Input API.
Сцена Assets/Scenes/Ship.unity создана редактором и добавляется вместе с .meta; геометрия появляется в Play Mode.
Геометрия появляется в Play Mode. Для полноценного редактирования сцены это впоследствии стоит заменить сохранёнными объектами и prefab.

## Известный технический долг
CourierShip.cs совмещает несколько обязанностей; материалы создаются многократно.
Нет физического контроллера, постоянного состояния и адаптивного UI. Для импорта персонажей добавлена batch-проверка реального Play Mode; полного набора регрессионных тестов пока нет.
Editor-инициализация теперь пропускает создание при dirty active scene; восстановление/импорт чистого checkout ещё проверить.
Runtime-поиск Standard shader может потребовать явного включения шейдера в сборку; проверить в настоящем player/WebGL.

## Направление архитектуры — предложение
Выделять по мере развития: ShipBuilder, PlayerController, InteractionController, Lift, OrderState, OrderView.
Состояние заказа отделить от отображения; данные заказов можно вынести в ScriptableObject после появления второго заказа.
Не добавлять DI-фреймворк, сетевой стек и сложную систему событий ради одного прототипа.

## Публикация
Рабочей проверенной публичной ссылки пока нет. Git push не является публикацией игры.
Godot-настройки Web не публикуют Unity-версию. GitHub Pages / CI пока не настроены.
Следующий кандидат — Unity WebGL, но сначала нужен локальный запуск и выбор поддерживаемой конфигурации редактора.
