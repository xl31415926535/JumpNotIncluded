World Three successful actual-input traces

These 25 CSV files accompany world-three-route-proof.txt: 14 lower-route Fire Flower
single-jump segments and 11 upper-route Monarch Wings segments. Each file records
a separate successful test segment on the intact authored map.

The harness stages an equipped Mario at a grounded launch position, allows natural
hazard phases, then drives the real Unity Input System and PlayerMotor. No in-flight
position editing, added invulnerability, disabled hazard or enemy removal is used.
Fire form is required to remain unchanged throughout the successful segment.

Each CSV starts with the route and exact hazard clocks at launch. Data columns are:
time (seconds since this segment began), x, y, vx, vy, grounded, move_input,
jump_input, air_jump_used, form, queued_move_input, queued_jump_input.
Positions and velocities are sampled from live physics. The queued columns record
the exact keyboard state sent to the Unity Input System. The original move_input
and jump_input columns retain InputAction observations at the editor callback;
these can differ from values consumed inside FixedUpdate and are not a replay of
the player's consumed inputs. In particular move_input can read zero while a
queued D input is moving the live body. Both observations are retained explicitly.

LIMIT: The staged launch and independent phase retry restart for EVERY segment.
These files are not a continuous level replay, human run or TAS completion. They
prove the listed individual jumps with live hazards. A continuous non-mech clear
has not been demonstrated. The separate core report does demonstrate an intact
full-map mech clear using D only with zero deaths and no Space/J input.
