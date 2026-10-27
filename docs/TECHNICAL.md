# Техническая база

## Репозиторий
Локальная папка: W:\Planets\Yantages.
Origin: git@github.com:kolumb718/Yantages.git.
Основная ветка на момент фиксации: main. Секретные ключи не хранить в репозитории.

| Путь | Назначение |
| --- | --- |
| unity/ | Новая основная версия |
| unity/Assets/Scripts/CourierShip.cs | Точка сборки сцены: создаёт модули и направляет ввод к героине, лифту, терминалу и камере |
| unity/Assets/Scripts/Core/ | SceneKit — материалы, примитивы, надписи и свет с владением ресурсами; ShipShaders — шейдеры, гарантированно попадающие в сборку |
| unity/Assets/Scripts/Input/CourierInput.cs | Input System: действия клавиатуры и геймпада, подписи клавиш активного устройства |
| unity/Assets/Scripts/Ship/ | ShipLayout — все размеры корабля; CourierMotor и FloorSweep — ходьба, границы и мебель-препятствия; OrderTerminal — заказ; ShipInterior — корпус, встроенная мебель и свет |
| unity/Assets/Scripts/Ship/GalaxyAtlas.cs | Каталог миров из JSON, навигация атласа и принятие общей с терминалом заявки |
| unity/Assets/Scripts/UI/GalaxyMapView.cs | Галактический интерфейс: отдельная камера, реальные текст/геометрия и экранные hit boxes |
| unity/Assets/Resources/Galaxy/planets.json | Черновой лор шести миров и заявки еды; первый заказ Нептуна берётся из DeliveryOrder.NeptuneSoup |
| unity/Assets/Editor/GalaxyIntegrationCheck.cs | Реальные события клавиатуры/мыши, рендер атласа и карточек, заказы, блокировка/возврат ходьбы, повторный Play Mode |
| unity/Assets/Scripts/Ship/Lift/ | ShipLift — поездка, пассажир, плавные двери, стены шахты; GlassLiftVisuals — стеклянная шахта и индикаторы |
| unity/Assets/Scripts/Camera/ShipCameraFraming.cs | Третье лицо: орбита, дистанция, приближение у препятствий; C — общий план; видимость этажей и параллакс |
| unity/Assets/Scripts/Scenery/ | Окно с городом, ангар с кораблём, надписи и голограмма, гардероб, клетка совы OwlAviary, размещение мебели из prefab |
| unity/Assets/Scripts/Characters/ | CourierCharacters — создание персонажей и скорость анимации Кристи; OwlCompanion — полёты, клетка, лифт |
| unity/Assets/Scripts/Characters/CourierOutfits.cs | Цветовые варианты существующей одежды, рюкзак на риге, примерка/отмена/подтверждение и PlayerPrefs косметики |
| unity/Assets/Scripts/Scenery/CabinWardrobe.cs | Личный гардероб на второй палубе, оформление через SceneKit |
| unity/Assets/Scripts/Scenery/LivingDeckDressing.cs, LivingGalleyDetails.cs | Бытовые детали гостиной, курьерское хранение и кухня; возвращают корни мебели для регистрации препятствий |
| unity/Assets/Editor/WardrobeIntegrationCheck.cs | Batch-проверка смены одежды через реальный игровой ввод; сохраняет прежний PlayerPrefs |
| unity/Assets/Editor/AnimatedCharacterSetup.cs, GaitClips.cs | Импорт риг-моделей, humanoid-аватар, генерация ходьбы и бега, контроллеры и prefab |
| unity/Assets/Scripts/UI/ShipHud.cs | IMGUI-интерфейс с масштабированием под экран и подписями клавиш устройства |
| unity/Assets/Resources/Shaders/ | WindowCity, Hologram, OrbitaUnlit и StandardReference.mat — включаются в сборку через Resources |
| unity/Assets/Art/Furniture/ | FBX, палитры, материалы и prefab мебели/растений |
| unity/Assets/Editor/FurnitureSetup.cs | Подготовка мебели и привязка в Ship |
| unity/Assets/Art/Characters/ | FBX, палитры, материалы и prefab героини/совы |
| unity/Assets/Editor/CharacterSetup.cs | Подготовка персонажей и привязка в Ship |
| unity/Assets/Editor/CharacterIntegrationCheck.cs | Batch-проверка Play Mode, лифта, ангара и рендеров |
| unity/Assets/Editor/ShipSystemsCheck.cs | Batch-проверка камеры, ввода (клавиатура и виртуальный геймпад), препятствий, дверей лифта, шейдеров сборки, анимации ходьбы/бега и отдыха совы в клетке |
| unity/Assets/Editor/GaitDirectionAudit.cs | Реальное семплирование направления опорной стопы: контроллер и HumanPoseHandler |
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
Используются Built-in renderer, Standard shader, IMGUI и Input System 1.20.1 (Active Input Handling — только новый ввод).
Сцена Assets/Scenes/Ship.unity создана редактором и добавляется вместе с .meta; геометрия появляется в Play Mode.
Геометрия появляется в Play Mode. Для полноценного редактирования сцены это впоследствии стоит заменить сохранёнными объектами и prefab.

## Известный технический долг
Нет физического контроллера: ходьба ограничена прямоугольниками палуб, стенами шахты и следами мебели. Нет постоянного состояния (сохранений). UI на IMGUI масштабируется, но навигация по кнопкам геймпадом в карточке заказа идёт через действия, а не фокус. Есть две batch-проверки; модульных тестов пока нет.
Editor-инициализация теперь пропускает создание при dirty active scene; восстановление/импорт чистого checkout ещё проверить.
Шейдеры закреплены в Resources (проверено ShipSystemsCheck в редакторе); настоящая player/WebGL-сборка ещё не собиралась.

## Направление архитектуры — предложение
Выполнено 25 октября 2026: CourierShip разделён на ShipInterior, CourierMotor, OrderTerminal, CourierInput и ShipHud. Дальше по мере роста: InteractionController при появлении третьего интерактивного объекта, данные заказов в ScriptableObject после второго заказа.
Состояние заказа отделить от отображения; данные заказов можно вынести в ScriptableObject после появления второго заказа.
Не добавлять DI-фреймворк, сетевой стек и сложную систему событий ради одного прототипа.

## Публикация
Рабочей проверенной публичной ссылки пока нет. Git push не является публикацией игры.
Godot-настройки Web не публикуют Unity-версию. GitHub Pages / CI пока не настроены.
Следующий кандидат — Unity WebGL, но сначала нужен локальный запуск и выбор поддерживаемой конфигурации редактора.
