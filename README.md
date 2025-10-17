This sits between Fusion 360 and Orcaslicer for my workflow. I have one datadir (specified using `--datadir`) per printer, and a shortcut for each one. This keeps everything separate and I don't need to switch printers or sync AMS in OrcaSlicer. it's just clean and simple.

OrcaLaunch detects running OrcaSlicer instances and pops up a button for each one, click the one you want to print to. The instances are identified via the last folder in their `--datadir` path, in this case `/toot` and `/zoot`

<img width="1087" height="233" alt="image" src="https://github.com/user-attachments/assets/c795fae2-fecb-4aad-84fb-e90930fb293c" />

To work properly this needs Single Instance, but by default OrcaSlicer does not distinguish between instances with different `--datadir` - this is a problem. 

My [fork](https://github.com/stuart11n/OrcaSlicer) modifies OrcaSlicer so it **does**. My modification is very small Windows only and probably has some side-effects, but it works for me. It would be great if OrcaSlicer could improve upon and incorporate it. 

Without the fork, you won't be able to enable Single Instance so it won't be as slick but it'll still work.
