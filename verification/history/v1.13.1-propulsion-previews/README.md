# War God propulsion checks

These are staged Unity QA fixtures, not a human playthrough. The helper grants an already purchased and deployed suit to isolate propulsion from the independently checked arrival sequence. It removes interfering enemies, preserves the original ground, pipes and gaps, and repositions the initial ground, pipe and abyss fixtures.

Walking, ascent, hover, cruise in both directions and landing use the real Input System keyboard. The images are rendered by the actual game camera at 1280x720. Assertions inspect grounded/cruising state, the imported animation frame, independently rendered exhaust and engine audio source. The helper restores editor run data, high score, Input System settings and background preference after execution.
