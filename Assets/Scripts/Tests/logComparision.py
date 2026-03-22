




# A Script to compare the logs at a single tick

logEntry1 = '''
--- tick=3 checksum=-224564294927175
  entityId[10] body[0] (Player) pos=(-127560528691,-19797079099) vel=(0,-3218005351) angle=0 angularVel=0
  entityId[11] body[1] (Player) pos=(-120774480364,-20140676482) vel=(0,-3218005351) angle=0 angularVel=0
  entityId[100] body[2] (Trap_Block_1x1(Clone)) pos=(-169651208192,-25769803776) vel=(0,0) angle=0 angularVel=0
  entityId[101] body[3] (Trap_Block_1x1(Clone)) pos=(-152041842278,-33930241638) vel=(0,0) angle=0 angularVel=0
  entityId[102] body[4] (Trap_Block_2x2(Clone)) pos=(-125284196024,-34273839022) vel=(0,0) angle=26790426361 angularVel=0
  entityId[103] body[5] (Trap_Block_2x2(Clone)) pos=(-107331232727,-34187939676) vel=(0,0) angle=239126613 angularVel=0
  entityId[104] body[6] (Trap_Block_1x1(Clone)) pos=(-102177271972,-46729244180) vel=(0,0) angle=665656527 angularVel=0
  entityId[105] body[7] (Trap_Block_2x2(Clone)) pos=(-89249420411,-52055003628) vel=(0,0) angle=0 angularVel=0
  entityId[106] body[8] (Trap_Block_3x1(Clone)) pos=(-85383949844,-39127152067) vel=(0,0) angle=234628933 angularVel=0
  entityId[107] body[9] (Trap_BalanceBeam(Clone)) pos=(-80144089743,-17222818857) vel=(0,0) angle=7185363302 angularVel=0
  entityId[108] body[10] (Trap_BalanceBeam(Clone)) pos=(-78941498900,-26371099197) vel=(0,0) angle=7989744814 angularVel=0
  entityId[109] body[11] (Trap_BalanceBeam(Clone)) pos=(-53257594470,-25125558682) vel=(0,0) angle=8238486073 angularVel=0
  entityId[110] body[12] (Trap_BalanceBeam(Clone)) pos=(-44109314130,-13700945674) vel=(0,0) angle=7056109106 angularVel=0
  entityId[111] body[13] (Trap_Block_3x1(Clone)) pos=(-43508018708,73787538145) vel=(0,0) angle=6819231333 angularVel=0
  entityId[112] body[14] (Trap_Block_1x1(Clone)) pos=(-43422119363,49263274885) vel=(0,0) angle=13946553693 angularVel=0
  entityId[113] body[15] (Trap_Block_1x1(Clone)) pos=(-43422119363,56908316672) vel=(0,0) angle=13946553693 angularVel=0
  entityId[114] body[16] (Trap_Block_3x1(Clone)) pos=(-42949672960,98741298135) vel=(0,0) angle=6938419833 angularVel=0
  entityId[115] body[17] (Trap_Block_3x1(Clone)) pos=(-42949672960,123695058125) vel=(0,0) angle=6763010343 angularVel=0
  entityId[116] body[18] (Trap_Block_3x1(Clone)) pos=(-36464272343,-33715493274) vel=(0,0) angle=665656527 angularVel=0
  entityId[117] body[19] (Trap_Block_3x1(Clone)) pos=(-26328149524,51024211476) vel=(0,0) angle=13946553693 angularVel=0
  entityId[118] body[20] (Trap_BalanceBeam(Clone)) pos=(-21775484191,-20916490732) vel=(0,0) angle=8139300188 angularVel=0
  entityId[119] body[21] (Trap_Block_3x1(Clone)) pos=(-11639361372,-29850022707) vel=(0,0) angle=665656527 angularVel=0
  entityId[120] body[22] (Trap_BalanceBeam(Clone)) pos=(-4724464026,-8074538516) vel=(0,0) angle=7056109106 angularVel=0
  entityId[121] body[23] (Trap_Block_3x1(Clone)) pos=(-816043786,53730040873) vel=(0,0) angle=13946553693 angularVel=0
  entityId[122] body[24] (Trap_Block_1x1(Clone)) pos=(3092376453,-110251810488) vel=(0,0) angle=665656527 angularVel=0
  entityId[123] body[25] (Trap_Block_3x1(Clone)) pos=(4123168604,-93415538688) vel=(0,0) angle=19704332727 angularVel=0
  entityId[124] body[26] (Trap_Block_3x1(Clone)) pos=(4896262717,-68676527063) vel=(0,0) angle=20325012461 angularVel=0
  entityId[125] body[27] (Trap_Block_3x1(Clone)) pos=(5841155523,-43980465111) vel=(0,0) angle=20114371151 angularVel=0
  entityId[126] body[28] (Trap_BalanceBeam(Clone)) pos=(11081015624,-16363825398) vel=(0,0) angle=8125358471 angularVel=0
  entityId[127] body[29] (Trap_Block_3x1(Clone)) pos=(13314398618,-25984552141) vel=(0,0) angle=665656527 angularVel=0
  entityId[128] body[30] (Trap_Block_3x1(Clone)) pos=(24309514895,56349970924) vel=(0,0) angle=13946553693 angularVel=0
  entityId[129] body[31] (Trap_BalanceBeam(Clone)) pos=(36034775613,-4209067950) vel=(0,0) angle=7056109106 angularVel=0
  entityId[130] body[32] (Trap_Block_3x1(Clone)) pos=(38654705664,-22033182228) vel=(0,0) angle=665656527 angularVel=0
  entityId[131] body[33] (Trap_Block_3x1(Clone)) pos=(49306224558,59012850647) vel=(0,0) angle=13946553693 angularVel=0
  entityId[132] body[34] (Trap_BalanceBeam(Clone)) pos=(55190329754,-11252814316) vel=(0,0) angle=7056109106 angularVel=0
  entityId[133] body[35] (Trap_Block_1x1(Clone)) pos=(55662776156,-19413252178) vel=(0,0) angle=665656527 angularVel=0
  entityId[134] body[36] (Trap_Block_3x1(Clone)) pos=(57552561766,75548474737) vel=(0,0) angle=6819231333 angularVel=0
  entityId[135] body[37] (Trap_Block_3x1(Clone)) pos=(58110907515,100502234726) vel=(0,0) angle=6620583833 angularVel=0
  entityId[136] body[38] (Trap_Block_3x1(Clone)) pos=(58110907515,125455994716) vel=(0,0) angle=6763010343 angularVel=0
  entityId[137] body[39] (Trap_Block_L(Clone)) pos=(104067057582,-19327352832) vel=(0,0) angle=56220990 angularVel=0
  entityId[138] body[40] (Trap_Block_2x2(Clone)) pos=(109178068664,-10222022164) vel=(0,0) angle=0 angularVel=0
  entityId[139] body[41] (Trap_Block_3x1(Clone)) pos=(122105920225,-14989435863) vel=(0,0) angle=20112871924 angularVel=0
  entityId[140] body[42] (Trap_Block_1x1(Clone)) pos=(138598594642,10995116278) vel=(0,0) angle=0 angularVel=0
  clientId=1 move=None jump=None wasPredicted=False
--- tick=4 checksum=-224553094735197
  entityId[10] body[0] (Player) pos=(-127560528691,-19828342919) vel=(0,-4288529208) angle=0 angularVel=0
  entityId[11] body[1] (Player) pos=(-120774480364,-20171940302) vel=(0,-4288529208) angle=0 angularVel=0
  entityId[100] body[2] (Trap_Block_1x1(Clone)) pos=(-169651208192,-25769803776) vel=(0,0) angle=0 angularVel=0
  entityId[101] body[3] (Trap_Block_1x1(Clone)) pos=(-152041842278,-33930241638) vel=(0,0) angle=0 angularVel=0
  entityId[102] body[4] (Trap_Block_2x2(Clone)) pos=(-125284196024,-34273839022) vel=(0,0) angle=26790426361 angularVel=0
  entityId[103] body[5] (Trap_Block_2x2(Clone)) pos=(-107331232727,-34187939676) vel=(0,0) angle=239126613 angularVel=0
  entityId[104] body[6] (Trap_Block_1x1(Clone)) pos=(-102177271972,-46729244180) vel=(0,0) angle=665656527 angularVel=0
  entityId[105] body[7] (Trap_Block_2x2(Clone)) pos=(-89249420411,-52055003628) vel=(0,0) angle=0 angularVel=0
  entityId[106] body[8] (Trap_Block_3x1(Clone)) pos=(-85383949844,-39127152067) vel=(0,0) angle=234628933 angularVel=0
  entityId[107] body[9] (Trap_BalanceBeam(Clone)) pos=(-80144089743,-17222818857) vel=(0,0) angle=7186596856 angularVel=0
  entityId[108] body[10] (Trap_BalanceBeam(Clone)) pos=(-78941498900,-26371099197) vel=(0,0) angle=7989579157 angularVel=0
  entityId[109] body[11] (Trap_BalanceBeam(Clone)) pos=(-53257594470,-25125558682) vel=(0,0) angle=8239440477 angularVel=0
  entityId[110] body[12] (Trap_BalanceBeam(Clone)) pos=(-44109314130,-13700945674) vel=(0,0) angle=7056109106 angularVel=0
  entityId[111] body[13] (Trap_Block_3x1(Clone)) pos=(-43508018708,73787538145) vel=(0,0) angle=6819231333 angularVel=0
  entityId[112] body[14] (Trap_Block_1x1(Clone)) pos=(-43422119363,49263274885) vel=(0,0) angle=13946553693 angularVel=0
  entityId[113] body[15] (Trap_Block_1x1(Clone)) pos=(-43422119363,56908316672) vel=(0,0) angle=13946553693 angularVel=0
  entityId[114] body[16] (Trap_Block_3x1(Clone)) pos=(-42949672960,98741298135) vel=(0,0) angle=6938419833 angularVel=0
  entityId[115] body[17] (Trap_Block_3x1(Clone)) pos=(-42949672960,123695058125) vel=(0,0) angle=6763010343 angularVel=0
  entityId[116] body[18] (Trap_Block_3x1(Clone)) pos=(-36464272343,-33715493274) vel=(0,0) angle=665656527 angularVel=0
  entityId[117] body[19] (Trap_Block_3x1(Clone)) pos=(-26328149524,51024211476) vel=(0,0) angle=13946553693 angularVel=0
  entityId[118] body[20] (Trap_BalanceBeam(Clone)) pos=(-21775484191,-20916490732) vel=(0,0) angle=8139300188 angularVel=0
  entityId[119] body[21] (Trap_Block_3x1(Clone)) pos=(-11639361372,-29850022707) vel=(0,0) angle=665656527 angularVel=0
  entityId[120] body[22] (Trap_BalanceBeam(Clone)) pos=(-4724464026,-8074538516) vel=(0,0) angle=7056109106 angularVel=0
  entityId[121] body[23] (Trap_Block_3x1(Clone)) pos=(-816043786,53730040873) vel=(0,0) angle=13946553693 angularVel=0
  entityId[122] body[24] (Trap_Block_1x1(Clone)) pos=(3092376453,-110251810488) vel=(0,0) angle=665656527 angularVel=0
  entityId[123] body[25] (Trap_Block_3x1(Clone)) pos=(4123168604,-93415538688) vel=(0,0) angle=19704332727 angularVel=0
  entityId[124] body[26] (Trap_Block_3x1(Clone)) pos=(4896262717,-68676527063) vel=(0,0) angle=20325012461 angularVel=0
  entityId[125] body[27] (Trap_Block_3x1(Clone)) pos=(5841155523,-43980465111) vel=(0,0) angle=20114371151 angularVel=0
  entityId[126] body[28] (Trap_BalanceBeam(Clone)) pos=(11081015624,-16363825398) vel=(0,0) angle=8125232081 angularVel=0
  entityId[127] body[29] (Trap_Block_3x1(Clone)) pos=(13314398618,-25984552141) vel=(0,0) angle=665656527 angularVel=0
  entityId[128] body[30] (Trap_Block_3x1(Clone)) pos=(24309514895,56349970924) vel=(0,0) angle=13946553693 angularVel=0
  entityId[129] body[31] (Trap_BalanceBeam(Clone)) pos=(36034775613,-4209067950) vel=(0,0) angle=7056109106 angularVel=0
  entityId[130] body[32] (Trap_Block_3x1(Clone)) pos=(38654705664,-22033182228) vel=(0,0) angle=665656527 angularVel=0
  entityId[131] body[33] (Trap_Block_3x1(Clone)) pos=(49306224558,59012850647) vel=(0,0) angle=13946553693 angularVel=0
  entityId[132] body[34] (Trap_BalanceBeam(Clone)) pos=(55190329754,-11252814316) vel=(0,0) angle=7056109106 angularVel=0
  entityId[133] body[35] (Trap_Block_1x1(Clone)) pos=(55662776156,-19413252178) vel=(0,0) angle=665656527 angularVel=0
  entityId[134] body[36] (Trap_Block_3x1(Clone)) pos=(57552561766,75548474737) vel=(0,0) angle=6819231333 angularVel=0
  entityId[135] body[37] (Trap_Block_3x1(Clone)) pos=(58110907515,100502234726) vel=(0,0) angle=6620583833 angularVel=0
  entityId[136] body[38] (Trap_Block_3x1(Clone)) pos=(58110907515,125455994716) vel=(0,0) angle=6763010343 angularVel=0
  entityId[137] body[39] (Trap_Block_L(Clone)) pos=(104067057582,-19327352832) vel=(0,0) angle=56220990 angularVel=0
  entityId[138] body[40] (Trap_Block_2x2(Clone)) pos=(109178068664,-10222022164) vel=(0,0) angle=0 angularVel=0
  entityId[139] body[41] (Trap_Block_3x1(Clone)) pos=(122105920225,-14989435863) vel=(0,0) angle=20112871924 angularVel=0
  entityId[140] body[42] (Trap_Block_1x1(Clone)) pos=(138598594642,10995116278) vel=(0,0) angle=0 angularVel=0
  clientId=1 move=None jump=None wasPredicted=False
'''

logEntry2 = '''
--- tick=3 checksum=-224564294927175
  entityId[10] body[0] (Player) pos=(-127560528691,-19797079099) vel=(0,-3218005351) angle=0 angularVel=0
  entityId[11] body[1] (Player) pos=(-120774480364,-20140676482) vel=(0,-3218005351) angle=0 angularVel=0
  entityId[100] body[2] (Trap_Block_1x1(Clone)) pos=(-169651208192,-25769803776) vel=(0,0) angle=0 angularVel=0
  entityId[101] body[3] (Trap_Block_1x1(Clone)) pos=(-152041842278,-33930241638) vel=(0,0) angle=0 angularVel=0
  entityId[102] body[4] (Trap_Block_2x2(Clone)) pos=(-125284196024,-34273839022) vel=(0,0) angle=26790426361 angularVel=0
  entityId[103] body[5] (Trap_Block_2x2(Clone)) pos=(-107331232727,-34187939676) vel=(0,0) angle=239126613 angularVel=0
  entityId[104] body[6] (Trap_Block_1x1(Clone)) pos=(-102177271972,-46729244180) vel=(0,0) angle=665656527 angularVel=0
  entityId[105] body[7] (Trap_Block_2x2(Clone)) pos=(-89249420411,-52055003628) vel=(0,0) angle=0 angularVel=0
  entityId[106] body[8] (Trap_Block_3x1(Clone)) pos=(-85383949844,-39127152067) vel=(0,0) angle=234628933 angularVel=0
  entityId[107] body[9] (Trap_BalanceBeam(Clone)) pos=(-80144089743,-17222818857) vel=(0,0) angle=7185363302 angularVel=0
  entityId[108] body[10] (Trap_BalanceBeam(Clone)) pos=(-78941498900,-26371099197) vel=(0,0) angle=7989744814 angularVel=0
  entityId[109] body[11] (Trap_BalanceBeam(Clone)) pos=(-53257594470,-25125558682) vel=(0,0) angle=8238486073 angularVel=0
  entityId[110] body[12] (Trap_BalanceBeam(Clone)) pos=(-44109314130,-13700945674) vel=(0,0) angle=7056109106 angularVel=0
  entityId[111] body[13] (Trap_Block_3x1(Clone)) pos=(-43508018708,73787538145) vel=(0,0) angle=6819231333 angularVel=0
  entityId[112] body[14] (Trap_Block_1x1(Clone)) pos=(-43422119363,49263274885) vel=(0,0) angle=13946553693 angularVel=0
  entityId[113] body[15] (Trap_Block_1x1(Clone)) pos=(-43422119363,56908316672) vel=(0,0) angle=13946553693 angularVel=0
  entityId[114] body[16] (Trap_Block_3x1(Clone)) pos=(-42949672960,98741298135) vel=(0,0) angle=6938419833 angularVel=0
  entityId[115] body[17] (Trap_Block_3x1(Clone)) pos=(-42949672960,123695058125) vel=(0,0) angle=6763010343 angularVel=0
  entityId[116] body[18] (Trap_Block_3x1(Clone)) pos=(-36464272343,-33715493274) vel=(0,0) angle=665656527 angularVel=0
  entityId[117] body[19] (Trap_Block_3x1(Clone)) pos=(-26328149524,51024211476) vel=(0,0) angle=13946553693 angularVel=0
  entityId[118] body[20] (Trap_BalanceBeam(Clone)) pos=(-21775484191,-20916490732) vel=(0,0) angle=8139300188 angularVel=0
  entityId[119] body[21] (Trap_Block_3x1(Clone)) pos=(-11639361372,-29850022707) vel=(0,0) angle=665656527 angularVel=0
  entityId[120] body[22] (Trap_BalanceBeam(Clone)) pos=(-4724464026,-8074538516) vel=(0,0) angle=7056109106 angularVel=0
  entityId[121] body[23] (Trap_Block_3x1(Clone)) pos=(-816043786,53730040873) vel=(0,0) angle=13946553693 angularVel=0
  entityId[122] body[24] (Trap_Block_1x1(Clone)) pos=(3092376453,-110251810488) vel=(0,0) angle=665656527 angularVel=0
  entityId[123] body[25] (Trap_Block_3x1(Clone)) pos=(4123168604,-93415538688) vel=(0,0) angle=19704332727 angularVel=0
  entityId[124] body[26] (Trap_Block_3x1(Clone)) pos=(4896262717,-68676527063) vel=(0,0) angle=20325012461 angularVel=0
  entityId[125] body[27] (Trap_Block_3x1(Clone)) pos=(5841155523,-43980465111) vel=(0,0) angle=20114371151 angularVel=0
  entityId[126] body[28] (Trap_BalanceBeam(Clone)) pos=(11081015624,-16363825398) vel=(0,0) angle=8125358471 angularVel=0
  entityId[127] body[29] (Trap_Block_3x1(Clone)) pos=(13314398618,-25984552141) vel=(0,0) angle=665656527 angularVel=0
  entityId[128] body[30] (Trap_Block_3x1(Clone)) pos=(24309514895,56349970924) vel=(0,0) angle=13946553693 angularVel=0
  entityId[129] body[31] (Trap_BalanceBeam(Clone)) pos=(36034775613,-4209067950) vel=(0,0) angle=7056109106 angularVel=0
  entityId[130] body[32] (Trap_Block_3x1(Clone)) pos=(38654705664,-22033182228) vel=(0,0) angle=665656527 angularVel=0
  entityId[131] body[33] (Trap_Block_3x1(Clone)) pos=(49306224558,59012850647) vel=(0,0) angle=13946553693 angularVel=0
  entityId[132] body[34] (Trap_BalanceBeam(Clone)) pos=(55190329754,-11252814316) vel=(0,0) angle=7056109106 angularVel=0
  entityId[133] body[35] (Trap_Block_1x1(Clone)) pos=(55662776156,-19413252178) vel=(0,0) angle=665656527 angularVel=0
  entityId[134] body[36] (Trap_Block_3x1(Clone)) pos=(57552561766,75548474737) vel=(0,0) angle=6819231333 angularVel=0
  entityId[135] body[37] (Trap_Block_3x1(Clone)) pos=(58110907515,100502234726) vel=(0,0) angle=6620583833 angularVel=0
  entityId[136] body[38] (Trap_Block_3x1(Clone)) pos=(58110907515,125455994716) vel=(0,0) angle=6763010343 angularVel=0
  entityId[137] body[39] (Trap_Block_L(Clone)) pos=(104067057582,-19327352832) vel=(0,0) angle=56220990 angularVel=0
  entityId[138] body[40] (Trap_Block_2x2(Clone)) pos=(109178068664,-10222022164) vel=(0,0) angle=0 angularVel=0
  entityId[139] body[41] (Trap_Block_3x1(Clone)) pos=(122105920225,-14989435863) vel=(0,0) angle=20112871924 angularVel=0
  entityId[140] body[42] (Trap_Block_1x1(Clone)) pos=(138598594642,10995116278) vel=(0,0) angle=0 angularVel=0
  clientId=0 move=None jump=JumpPressed wasPredicted=False
--- tick=4 checksum=-224997632597177
  entityId[10] body[0] (Player) pos=(-127560528691,-19229463375) vel=(0,67577016324) angle=0 angularVel=0
  entityId[11] body[1] (Player) pos=(-120774480364,-20171940302) vel=(0,-4288529208) angle=0 angularVel=0
  entityId[100] body[2] (Trap_Block_1x1(Clone)) pos=(-169651208192,-25769803776) vel=(0,0) angle=0 angularVel=0
  entityId[101] body[3] (Trap_Block_1x1(Clone)) pos=(-152041842278,-33930241638) vel=(0,0) angle=0 angularVel=0
  entityId[102] body[4] (Trap_Block_2x2(Clone)) pos=(-125284196024,-34273839022) vel=(0,0) angle=26790426361 angularVel=0
  entityId[103] body[5] (Trap_Block_2x2(Clone)) pos=(-107331232727,-34187939676) vel=(0,0) angle=239126613 angularVel=0
  entityId[104] body[6] (Trap_Block_1x1(Clone)) pos=(-102177271972,-46729244180) vel=(0,0) angle=665656527 angularVel=0
  entityId[105] body[7] (Trap_Block_2x2(Clone)) pos=(-89249420411,-52055003628) vel=(0,0) angle=0 angularVel=0
  entityId[106] body[8] (Trap_Block_3x1(Clone)) pos=(-85383949844,-39127152067) vel=(0,0) angle=234628933 angularVel=0
  entityId[107] body[9] (Trap_BalanceBeam(Clone)) pos=(-80144089743,-17222818857) vel=(0,0) angle=7186596856 angularVel=0
  entityId[108] body[10] (Trap_BalanceBeam(Clone)) pos=(-78941498900,-26371099197) vel=(0,0) angle=7989579157 angularVel=0
  entityId[109] body[11] (Trap_BalanceBeam(Clone)) pos=(-53257594470,-25125558682) vel=(0,0) angle=8239440477 angularVel=0
  entityId[110] body[12] (Trap_BalanceBeam(Clone)) pos=(-44109314130,-13700945674) vel=(0,0) angle=7056109106 angularVel=0
  entityId[111] body[13] (Trap_Block_3x1(Clone)) pos=(-43508018708,73787538145) vel=(0,0) angle=6819231333 angularVel=0
  entityId[112] body[14] (Trap_Block_1x1(Clone)) pos=(-43422119363,49263274885) vel=(0,0) angle=13946553693 angularVel=0
  entityId[113] body[15] (Trap_Block_1x1(Clone)) pos=(-43422119363,56908316672) vel=(0,0) angle=13946553693 angularVel=0
  entityId[114] body[16] (Trap_Block_3x1(Clone)) pos=(-42949672960,98741298135) vel=(0,0) angle=6938419833 angularVel=0
  entityId[115] body[17] (Trap_Block_3x1(Clone)) pos=(-42949672960,123695058125) vel=(0,0) angle=6763010343 angularVel=0
  entityId[116] body[18] (Trap_Block_3x1(Clone)) pos=(-36464272343,-33715493274) vel=(0,0) angle=665656527 angularVel=0
  entityId[117] body[19] (Trap_Block_3x1(Clone)) pos=(-26328149524,51024211476) vel=(0,0) angle=13946553693 angularVel=0
  entityId[118] body[20] (Trap_BalanceBeam(Clone)) pos=(-21775484191,-20916490732) vel=(0,0) angle=8139300188 angularVel=0
  entityId[119] body[21] (Trap_Block_3x1(Clone)) pos=(-11639361372,-29850022707) vel=(0,0) angle=665656527 angularVel=0
  entityId[120] body[22] (Trap_BalanceBeam(Clone)) pos=(-4724464026,-8074538516) vel=(0,0) angle=7056109106 angularVel=0
  entityId[121] body[23] (Trap_Block_3x1(Clone)) pos=(-816043786,53730040873) vel=(0,0) angle=13946553693 angularVel=0
  entityId[122] body[24] (Trap_Block_1x1(Clone)) pos=(3092376453,-110251810488) vel=(0,0) angle=665656527 angularVel=0
  entityId[123] body[25] (Trap_Block_3x1(Clone)) pos=(4123168604,-93415538688) vel=(0,0) angle=19704332727 angularVel=0
  entityId[124] body[26] (Trap_Block_3x1(Clone)) pos=(4896262717,-68676527063) vel=(0,0) angle=20325012461 angularVel=0
  entityId[125] body[27] (Trap_Block_3x1(Clone)) pos=(5841155523,-43980465111) vel=(0,0) angle=20114371151 angularVel=0
  entityId[126] body[28] (Trap_BalanceBeam(Clone)) pos=(11081015624,-16363825398) vel=(0,0) angle=8125232081 angularVel=0
  entityId[127] body[29] (Trap_Block_3x1(Clone)) pos=(13314398618,-25984552141) vel=(0,0) angle=665656527 angularVel=0
  entityId[128] body[30] (Trap_Block_3x1(Clone)) pos=(24309514895,56349970924) vel=(0,0) angle=13946553693 angularVel=0
  entityId[129] body[31] (Trap_BalanceBeam(Clone)) pos=(36034775613,-4209067950) vel=(0,0) angle=7056109106 angularVel=0
  entityId[130] body[32] (Trap_Block_3x1(Clone)) pos=(38654705664,-22033182228) vel=(0,0) angle=665656527 angularVel=0
  entityId[131] body[33] (Trap_Block_3x1(Clone)) pos=(49306224558,59012850647) vel=(0,0) angle=13946553693 angularVel=0
  entityId[132] body[34] (Trap_BalanceBeam(Clone)) pos=(55190329754,-11252814316) vel=(0,0) angle=7056109106 angularVel=0
  entityId[133] body[35] (Trap_Block_1x1(Clone)) pos=(55662776156,-19413252178) vel=(0,0) angle=665656527 angularVel=0
  entityId[134] body[36] (Trap_Block_3x1(Clone)) pos=(57552561766,75548474737) vel=(0,0) angle=6819231333 angularVel=0
  entityId[135] body[37] (Trap_Block_3x1(Clone)) pos=(58110907515,100502234726) vel=(0,0) angle=6620583833 angularVel=0
  entityId[136] body[38] (Trap_Block_3x1(Clone)) pos=(58110907515,125455994716) vel=(0,0) angle=6763010343 angularVel=0
  entityId[137] body[39] (Trap_Block_L(Clone)) pos=(104067057582,-19327352832) vel=(0,0) angle=56220990 angularVel=0
  entityId[138] body[40] (Trap_Block_2x2(Clone)) pos=(109178068664,-10222022164) vel=(0,0) angle=0 angularVel=0
  entityId[139] body[41] (Trap_Block_3x1(Clone)) pos=(122105920225,-14989435863) vel=(0,0) angle=20112871924 angularVel=0
  entityId[140] body[42] (Trap_Block_1x1(Clone)) pos=(138598594642,10995116278) vel=(0,0) angle=0 angularVel=0
  clientId=0 move=None jump=None wasPredicted=False
'''

def compare_log_entries(entry1, entry2):
    """Compare two log entries line by line and print differences."""
    
    # Split into lines and strip whitespace
    lines1 = [line.strip() for line in entry1.strip().split('\n') if line.strip()]
    lines2 = [line.strip() for line in entry2.strip().split('\n') if line.strip()]
    
    # Check if they have the same number of lines
    if len(lines1) != len(lines2):
        print(f"WARNING: Different number of lines! Entry1: {len(lines1)}, Entry2: {len(lines2)}")
        print()
    
    # Compare line by line
    differences_found = False
    max_lines = max(len(lines1), len(lines2))
    
    for i in range(max_lines):
        # Get lines, or use empty string if one log is shorter
        line1 = lines1[i] if i < len(lines1) else "<MISSING>"
        line2 = lines2[i] if i < len(lines2) else "<MISSING>"
        
        if line1 != line2:
            differences_found = True
            print(f"Line {i+1} differs:")
            print(f"  Entry1: {line1}")
            print(f"  Entry2: {line2}")
            print()
    
    # Print result
    if not differences_found:
        print("✓ Strings are equal!")
    else:
        print(f"❌ Found differences in the log entries")
 
if __name__ == "__main__":
    compare_log_entries(logEntry1, logEntry2)