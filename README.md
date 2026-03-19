# Axiome's Toolbox
A Celeste mod adding features primarily intended for speedrun practice and labbing

## Features

### Freeze the timer when the game is paused

Testing movement and strat alternatives often requires frame advance and pause buffering, but the timer running during pause causes timing inaccuracies. This option halts the timer while the game is paused, ensuring accurate timing.

### Checkpoint

Similar to srt endpoint but doesn't stop the timer. They count as a room in regards to srt room count.

### Detection rules inspired by [MovementLinter](https://github.com/6point28etc/MovementLinter)

- Bad cornerboost detection
- Frames lost to menu actions (skip cutscene, retry, S&Q and RTM)
- Frames lost to late death confirm
- Failed waterboost
