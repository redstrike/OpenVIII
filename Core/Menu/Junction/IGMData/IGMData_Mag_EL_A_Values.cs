﻿using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace OpenVIII
{
    public partial class Junction
    {
        #region Classes

        private class IGMData_Mag_EL_A_Values : IGMData_Values
        {
            #region Methods

            public static IGMData_Mag_EL_A_Values Create() => Create<IGMData_Mag_EL_A_Values>(8, 5, new IGMDataItem.Box { Title = Icons.ID.Elemental_Attack, Pos = new Rectangle(280, 423, 545, 201) }, 2, 4);

            public Dictionary<Kernel.Element, byte> getTotal(Saves.CharacterData source, out Enum[] availableFlagsarray)
                    => getTotal<Kernel.Element>(out availableFlagsarray, 200, Kernel.Stat.ElAtk, source.StatJ[Kernel.Stat.ElAtk]);

            public override bool Update()
            {
                if (Memory.State?.Characters != null && Damageable!=null && Damageable.GetCharacterData(out var c))
                {
                    var oldtotal = (prevSetting != null) ? getTotal(prevSetting, out var availableFlagsarray) : null;
                    var total = getTotal(c, out availableFlagsarray);
                    FillData(oldtotal, total, availableFlagsarray, Icons.ID.Element_Fire, palette: 9);
                }
                return base.Update();
            }

            protected override void InitShift(int i, int col, int row)
            {
                base.InitShift(i, col, row);
                SIZE[i].Inflate(-25, -25);
                SIZE[i].Y -= 6 * row;
            }

            #endregion Methods
        }

        #endregion Classes
    }
}