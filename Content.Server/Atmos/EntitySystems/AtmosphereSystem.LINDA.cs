#nullable disable warnings
#nullable enable annotations
using Content.Server.Atmos.Components;
using Content.Shared.Atmos;

namespace Content.Server.Atmos.EntitySystems
{
    public partial class AtmosphereSystem
    {
        private void ProcessCell(GridAtmosphereComponent gridAtmosphere, TileAtmosphere tile, int fireCount, bool spaceWind = true)
        {
            // Can't process a tile without air
            if (tile.Air == null)
            {
                gridAtmosphere.RemoveActiveTile(tile);
                return;
            }

            if (tile.ArchivedCycle < fireCount)
                Archive(tile, fireCount);

            tile.CurrentCycle = fireCount;
            var adjacentTileLength = 0;

            for (var i = 0; i < Atmospherics.Directions; i++)
            {
                var direction = (AtmosDirection) (1 << i);
                if(tile.AdjacentBits.IsFlagSet(direction))
                    adjacentTileLength++;
            }

            for(var i = 0; i < Atmospherics.Directions; i++)
            {
                var direction = (AtmosDirection) (1 << i);
                if (!tile.AdjacentBits.IsFlagSet(direction)) continue;
                var enemyTile = tile.AdjacentTiles[i];

                // If the tile is null or has no air, we don't do anything for it.
                if(enemyTile?.Air == null) continue;
                if (fireCount <= enemyTile.CurrentCycle) continue;
                Archive(enemyTile, fireCount);

                var shouldShareAir = false;

                if (tile.ExcitedGroup != null && enemyTile.ExcitedGroup != null)
                {
                    if (tile.ExcitedGroup != enemyTile.ExcitedGroup)
                    {
                        tile.ExcitedGroup.MergeGroups(enemyTile.ExcitedGroup);
                    }

                    shouldShareAir = true;
                } else if (tile.Air!.Compare(enemyTile.Air!) != GasMixture.GasCompareResult.NoExchange)
                {
                    if (!enemyTile.Excited)
                    {
                        gridAtmosphere.AddActiveTile(enemyTile);
                    }

                    var excitedGroup = tile.ExcitedGroup;
                    excitedGroup ??= enemyTile.ExcitedGroup;

                    if (excitedGroup == null)
                    {
                        excitedGroup = new ExcitedGroup();
                        excitedGroup.Initialize(gridAtmosphere);
                    }

                    if (tile.ExcitedGroup == null)
                        excitedGroup.AddTile(tile);

                    if(enemyTile.ExcitedGroup == null)
                        excitedGroup.AddTile(enemyTile);

                    shouldShareAir = true;
                }

                if (shouldShareAir)
                {
                    var difference = Share(tile.Air!, enemyTile.Air!, adjacentTileLength);

                    if (spaceWind)
                    {
                        if (difference > 0)
                        {
                            ConsiderPressureDifference(gridAtmosphere, tile, enemyTile, difference);
                        }
                        else
                        {
                            ConsiderPressureDifference(gridAtmosphere, enemyTile, tile, -difference);
                        }
                    }

                    LastShareCheck(tile);
                }
            }

            if(tile.Air != null)
                React(tile.Air, tile);
            tile.UpdateVisuals();

            var remove = true;

            if(tile.Air!.Temperature > Atmospherics.MinimumTemperatureStartSuperConduction)
                if (ConsiderSuperconductivity(gridAtmosphere, tile, true))
                    remove = false;

            if(tile.ExcitedGroup == null && remove)
                gridAtmosphere.RemoveActiveTile(tile);
        }

        private void Archive(TileAtmosphere tile, int fireCount)
        {
            tile.Air?.Archive();
            tile.ArchivedCycle = fireCount;
            tile.TemperatureArchived = tile.Temperature;
        }

        private void LastShareCheck(TileAtmosphere tile)
        {
            if (tile.Air == null || tile.ExcitedGroup == null)
                return;

            switch (tile.Air.LastShare)
            {
                case > Atmospherics.MinimumAirToSuspend:
                    tile.ExcitedGroup.ResetCooldowns();
                    break;
                case > Atmospherics.MinimumMolesDeltaToMove:
                    tile.ExcitedGroup.DismantleCooldown = 0;
                    break;
            }
        }
    }
}
