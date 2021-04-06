# Vita game dumps

Скачал игру и не знаешь что с ней делать? Тебе сюда!

Сам себе Шерлок Холмс...
...или гайд по распознаванию игр

1. Если игра скачана в виде одного файла с расширением VPK, то это Vitamin дамп. Необходимо установить этот файл при помощи molecularShell или VitaShell.

2. Если игра скачана в виде файла с расширением VPK и с дополнительным архивом в комплекте, то это адаптированный для небольших карт памяти Vitamin дамп (для установки требуется свободный объём памяти, равный размеру VPK файла). Необходимо установить файл VPK при помощи molecularShell или VitaShell, а после этого распаковать архив и скопировать его содержимое в соответствующую папку.

3. Если игра скачана в виде папки с файлами (с именем вида ABCD12345, т.е. четыре буквы и пять цифр), то тут может быть множество вариантов. Рассмотрим их:
	1. Если внутри папки с кодом есть папка "sce_pfs", то это NoNpDrm дамп. Такие дампы устанавливаются при помощи плагина NoNpDrm и программы VitaShell. Инструкция здесь.

	2. Если внутри папки с кодом нет папки "sce_pfs", но есть папки "RO" и "RW", то это NoPsmDrm дамп. Такие дампы устанавливаются при помощи плагина NoPsmDrm и программы EasyPowerRefresh. Инструкция здесь.

	3. Если внутри папки с кодом нет папки "sce_pfs", но есть файл "sce_module/steroid.suprx", то это Vitamin дамп. Такие дампы устанавливаются при помощи программы VitaShell (функция "Install Folder").

	Контрольные суммы SHA-1 для разных версий файла:
	- Vitamin	0.8	8CC41B5A4FD1397CBF3642641F6D654468F4BAAD
	- Vitamin	1.0/1.1	2B2FAA4BF7897BD62AE1E139BEC2211639BDE975
	- Vitamin	2.0	4371D5574F15BBFAE70BB9A7D98354E5022C7979

	4. Если внутри папки с кодом нет папки "sce_pfs", но есть файл "mai_moe/mai.suprx", то это Mai дамп. Такие дампы устанавливаются при помощи программы MaiDumpTool. Инструкция здесь.

	Контрольные суммы SHA-1 для разных версий файла:
	- MaiDumpTool	v233.0	A155B682D3803FA77EB5624ADCCFB03A47FE53A0
	- MaiDumpTool	v233.1	CF7BAB1B68353218BA0C8A6AF0A7A72C693C8313
	- MaiDumpTool	v233.2b2/2b7	E6D6955198F00BE4E32EE87EA737BB48BD56ABD8
	- MaiDumpTool	v233.2z2	283CF9D06654AD28AF9A04A7A35C754A0F6D8411
	- MaiDumpTool	v233.2z7/2z8/2z9	B80A485F00ABDCC79C0C2348C1D5CB1D26F7DCFF
	- MaiDumpTool	v233.2z10/2z10v2/2zEx	9B1ED3A379306062F65FECDCA48A671971934513

https://www.pspx.ru/forum/showthread.php?t=107300

## Game origins

1. PS Vita Gamecards
https://playstationdev.wiki/psvitadevwiki/index.php?title=Media
Game data is stored on the Gamecard. After sticking the Gamecard into Vita `ux0:appmeta/GAME`, a kind of "shortcut" is created, and a bubble with the game icon is created on the LiveArea. This bubble doesn't work without a Gamecard. 

2. PS Store. Links like: http://zeus.dl.playstation.net/cdn/UP1018/PCSE00442_00/<name>.pkg
Complete list of known links: https://nopaystation.com/
pkg is downloaded and installed as `ux0:app/GAME`.
