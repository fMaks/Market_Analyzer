# Market_Analyzer
Для анализа рынка на крипто-биржах. На данный момент (для экспериментов) используется только Huobi.
v.0.1.2

## Планы:
* Поиск различных паттернов
* Отсылка сигналов в Telegram

## Связь
Конструктивную критику, пожелания, предложения по улучшению кода и прочее слать на market.analyzer555DOGEgmail.com

## История изменений:

* 03.09.2023 Добавлен бот для отправки приватных сообщений, зачатки функционала. Мелкие правки.
* 19.02.2023 Добавлен конфиг (тип ini), историю с сервера теперь подгружает не все 2000, а сколько необходимо (по прежнему максимум 2000), лог-файл, мелкие правки
* 04.02.2023 Переписал соединение по вебсокету.
* 29.01.2023 Пробы пера. Лишь бы запускалось и работало. Получает свечные тики, сохраняет в файлы, читает из файлов, подтягивает историю 1М (2000 свечей, ограничение Huobi).