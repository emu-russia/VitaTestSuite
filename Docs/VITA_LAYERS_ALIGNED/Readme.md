# VITA_LAYERS_ALIGNED

Aligned PS Vita PCB layers.

There are actually 10 metal layers, I renamed Side B as LAYER 00 and SideA as LAYER 11 for convinience.

Source:

https://www.ifixit.com/Teardown/PlayStation+Vita+Teardown/7872

https://www.vitadevwiki.com/index.php?title=Layers

https://yadi.sk/d/CRDsbhJBmpmAX

I just aligned it and flipped SideB.

## PS Vita PCB Traced

Since PS Vita service manuals don't leak (well done SONY), and I want to delve into this wonderful system, I decided to start by drawing up a PS Vita block diagram so that aspiring Vita hackers can quickly start to explore this device.

In order to do so I traced most of PCB:

![image](https://user-images.githubusercontent.com/5828819/87879319-95294580-c9f2-11ea-883f-e02d82d96a3e.png)

## How to use

- Run the Deroute tool (https://github.com/ogamespec/psxdev/releases/tag/deroute-release-100)
- File -> Add entities
- Choose XML and open it
- Set wire size = 3 in settings
- Select wire
- Press F10
- Have fun

![image](https://user-images.githubusercontent.com/5828819/87879125-69599000-c9f1-11ea-8406-11f956d05daf.png)

## Release Notes 0.01

Most of the work has been done, but there is no detailed information on the contacts of ICs and connectors. It is planned to refine the tracing over time.

## Original images

PCB source images can be found in this repository (VITA_LAYERS_ALIGNED).

If necessary, they can also be loaded into the Deroute tool to clarify the information on the layout (File -> Load Image).
