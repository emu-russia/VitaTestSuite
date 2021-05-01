# Гайд по анбрику PS Vita

## Вступительное слово

Данный раздел содержит информацию для тех, кто попытался сделать то, что написано в https://vita.hacks.guide/, но по причине кривых рук каким-то образом умудрился брикнуть Виту в процессе.

Вообще говоря брикнуть (от англ. `brick`) или "окирпичить" Виту достаточно сложно. Внутри Виты на встроенной eMMC находится заводская прошивка, которую обычными способами сложно похерить.
Всё плохое, что обычно происходит во время экспериментов с прошивкой Виты называется "Soft Brick", то есть "программный кирпич".

Данный гайд предназначен именно для программного анбрика. Если вы хакер из Team Molecule и умудрились брикнуть eMMC, то этот гайд не для вас. В вашем случае можно добавиться в "Зал Позора" https://playstationdev.wiki/psvitadevwiki/index.php?title=Wall_of_Shame 

## Технические детали обновления прошивки

В процессе обновления Вита запрашивает у Content Manager специальный файл формата XML, psp2-updatelist.xml, который имеет примерно следующее содержание:

```
<?xml version="1.0" encoding="UTF-8"?>
<update_data_list>
<region id="us">
<np level0_system_version="01.600.000" level1_system_version="03.700.000" level2_system_version="03.700.000" map="03.700.000" />
<np_d level0_system_version="01.600.000" level1_system_version="03.700.000" level2_system_version="03.700.000" map="03.700.000" />
<version system_version="03.700.000" label="3.70">
<update_data update_type="full">
<image size="133770752">http://dus01.psp2.update.playstation.net/update/psp2/image/2019_0109/rel_54227fd645fcb6f177102cbd1f2d1924/PSP2UPDAT.PUP?dest=us</image>
</update_data>
</version>
<recovery spkg_type="systemdata">
<image spkg_version="01.000.010" size="56768512">http://dus01.psp2.update.playstation.net/update/psp2/image/2019_0109/sd_28b537fa2508afc523038030fd3d624f/PSP2UPDAT.PUP?dest=us</image>
</recovery>
<recovery spkg_type="preinst">
<image spkg_version="01.000.000" size="128788480">http://dus01.psp2.update.playstation.net/update/psp2/image/2019_0109/pre_af18f72827180188a00ec63196e31c4b/PSP2UPDAT.PUP?dest=us</image>
</recovery>
</region>
</update_data_list>
```

Основные сущности, которые вам потребуются:
- region id: Указывает регион для которой применяется прошивка
- level0_system_version и подобные: содержат версии прошивки, их желательно не менять
- label: Название прошивки, которое показывается на экране во время обновления
- update_type="full": Содержит URL для прошивки PUP, которая применяется при обычном обновлении (из Настроек или в других случаях, когда Вита находится в рабочем режиме). Для данного гайда данный способ не подходит.
- recovery: Содержит URL для прошивок, которые устанавливаются в режиме Recovery. Именно эти прошивки мы и будем использовать для программного анбрика в Recovery Menu

## Ошибки

Перед процессом анбрика несколько слов про ошибки, которые могут произойти во время анбрика. Если вы всё делаете правильно, по идее ошибок быть не должно.

Вот наиболее частые ошибки:
- C3-10698-4: Язык региона (region id) не соответствует региону вашей PS Vita.
- C3-12027-2 на 7%: Происходит когда в режиме Recovery вы пытаетесь прошиться прошивкой типа update_type="full".

## Content Manager Assistant и Qcma

Родное приложение SONY для прошивки PS Vita через ПК называется Content Manager Assistant. Для связи с ним требуется соединить PS Vita по USB с ПК и дальше они начнут обмениваться данными.
Данная программа нам не подходит, поскольку в ней нет возможности регулировать процессы.

Для анбрика нам потребуется Qcma: https://codestation.github.io/qcma/

Qcma позволяет контролировать процесс прошивки.

## Переход в Recovery Menu

Если ваша PS Vita брикнулась, то она должна автоматически переходить в режим Recovery.

Если вам нужно принудительно загрузиться в режиме Recovery нужно при включении PS Vita зажать одновременно PS Button + Right Shift.

Когда PS Vita брикнута, то все пункты кроме `5. Update System Software` не будут доступны. Для анбрика требуется использовать пункт 5 и обновление с помощью ПК (`Connecting to a PC`).

https://www.xtremepsvita.com/2012/07/17/activating-the-ps-vitas-safe-mode-recovery-menu/

## Настройка Qcma

Настройки Qcma нужно сделать такими:

![QcmaSettings](QcmaSettings.png)

Немного комментариев:
- Offline mode: Данная настройка требуется для того, чтобы Qcma не лезла на серверы SONY для скачивания прошивок (PUP), а брала их из папки `C:\\Users\User\Documents\PSV Updates` (или где вы указали данную папку в настройках Qcma).
- Ignore local file psp2-updatelist: Данную настройку необходимо отключить, чтобы Qcma передавала PS Vita файл psp2-updatelist из папки `PSV Updates`
- Use this version for updates: Custom + 03.710.000. Необходимо указать версию прошивки для восстановления, которую вы будете использовать. Указанная версия должна соответствовать тому, что написано в psp2-updatelist.

Настройки Qcma нужно сделать один раз, а потом закрыть графическую версию приложения.

Для контролирования процесса рекомендуется использовать консольную версию Qcma (qcma_console.exe) с опцией Verbose:

```
cd %PROGRAMFILES%\Qcma
qcma_console.exe --verbose
```

## Скачивание и подготовка PUP

Теперь нужно скачать все необходимые PUP, для версии прошивки, которую вы указали в настройках Qcma.

Проблема с PUP в том, что все они имеют одинаковые названия: `PSP2UPDAT.PUP`.

В режиме Offline Qcma обрезает URL вида `http://dru01.psp2.update.playstation.net/update/psp2/image/2019_0625/rel_46b79772448291194608b44b17235da2/PSP2UPDAT.PUP?dest=ru` до `PSP2UPDAT.PUP` и передает данный файл PS Vita.

А так как при прошивке из Recovery Menu требуется два файла PUP: systemdata и preinst необходимо после скачивания этих PUP в папку `PSV Updates` переименовать их, например как `PSP2UPDAT-SYS.PUP` и `PSP2UPDAT-PRE.PUP`

## Подготовка psp2-updatelist

В исходном psp2-updatelist необходимо поменять имена systemdata и preinst прошивок, чтобы Qcma передал PS Vita корректные файлы.

Пример исправленного psp2-updatelist.xml:

```
<recovery spkg_type="systemdata">
<image spkg_version="01.000.010" size="56768512">http://dru01.psp2.update.playstation.net/update/psp2/image/2019_0625/sd_24f9c20052b3ff8b1b1f62da290c11c1/PSP2UPDAT-SYS.PUP?dest=ru</image>
</recovery>
<recovery spkg_type="preinst">
<image spkg_version="01.000.000" size="128788480">http://dru01.psp2.update.playstation.net/update/psp2/image/2019_0625/pre_2efd6510bff08dd33b186c422789b200/PSP2UPDAT-PRE.PUP?dest=ru</image>
</recovery>
```

## Прошивка из Recovery Menu

Теперь нужно соединить PS Vita, запустить qcma_console и выбрать в Recovery Menu обновление через ПК.

Recovery Menu вначале запросит psp2-updatelist.xml, который Qcma отправит из папки `PSV Updates`.

Затем Recovery Menu последовательно запросит systemdata и preinst, по URL, которые указаны в psp2-updatelist.xml. Поскольку Qcma работает Offline, она обрежет данные URL и отправит требуемые PUP файлы из папки `PSV Updates`.

Когда процент дойдет до 100% ваша PS Vita перезапустится и будет анбрикнута.

## Если после прошивки PS Vita перезапускается но черный экран

Попробуйте вытащить 3G-модуль (если у вас 3G-версия PS Vita). В некоторых случаях неисправный 3G-модуль не дает нормально загрузиться.
