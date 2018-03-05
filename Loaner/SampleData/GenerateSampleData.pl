#!/usr/bin/perl -w

use strict;
use warnings FATAL => 'all';
die "You must provide the number of records to create. \n usage: GenerateSampleData.pl <Total Number of Records> <First Portfolio Size>" if @ARGV != 2;
my $NumberOfRecords = $ARGV[0];
my $RecordsPerPortfolio = $ARGV[1];

my $client =  join '', map +(q(A)..q(Z))[rand(26)], 1..10;
open(CLIENT,">Client-$client.txt") or die "$!\n";
open(OBLIGATIONS, ">Obligations/Client-$client.txt") or die "$!\n";
my $obligationCounter = 1;
my @Chars = ('1'..'9');
my $Length = 11;
my %unique=();
my $portfolio_splitter = 0;
my $account_number = 10000000000;
my @inventory = (
    'Studio Delux',
    '1 Bedroom Suite',
    '2 Bedroom Delux',
    '10000 points',
    '30000 points',
    '40000 points',
    '50000 points'
    );
my @daysDelinquent = (0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,135,145,123,120);
my @delinquentAmount = (-20,-50,-400,-23);
my @portfolioNames = (
'JacksonHole',
'Rose',
'PuebloBonito',
'GranRegina',
'WildernessLodge',
'JamboHouse',
'KidaniVillage',
'BeachClubVillas',
'BoardWalkVillas',
'Anaheim',
'AngelFire',
'AngelFireCabinShareI',
'AngelsCamp',
'AsiaPacificSurfersParadise',
'AsiaPacificTorquay',
'Austin',
'AvenuePlaza',
'BaliHaiVillas',
'BayClubII',
'BayVoyageInn',
'BeachStreetCottages',
'BentleyBrook',
'BisonRanch',
'BonnetCreek',
'BostonBeaconHill',
'BransonatTheFalls',
'BransonatTheMeadows',
'CanterburyatSanFrancisco',
'ClearwaterBeach',
'CypressPalms',
'DesertBlue',
'Durango',
'DyeVillasatMyrtleBeach',
'EmeraldGrandeatDestin',
'Flagstaff',
'FozdoIguau',
'Galena',
'GovernorsGreen',
'GrandChicagoRiverfront',
'GrandDesert',
'GrandLake',
'GrandPittsburghDowntown',
'GreatSmokiesLodge',
'HarbourLights',
'Indio',
'InnonLongWharf',
'InnontheHarbor',
'KaEoKai',
'KauaiBeachVillas',
'KingCottonVillas',
'Kingsgate',
'KonaHawaiian',
'LaBelleMaison',
'LaCascada',
'LakeMarion',
'LakeoftheOzarks',
'LongWharf',
'MaunaLoaVillage',
'MidtownatNewYorkCity',
'MountainVista',
'Nashville',
'NewportOnshore',
'NewportOverlook',
'OceanBoulevard',
'OceanRidge',
'OceanWalk',
'OceansidePier',
'OldTownAlexandria',
'Pagosa',
'PalmAire',
'PanamaCityBeach',
'ParkCity',
'PatriotsPlace',
'Pinetop',
'PratagyBeach',
'RamadaCoffsHarbourTreetops',
'RamadaGoldenBeach',
'RamadaWanaka',
'RamadaevenMileBeach',
'RanchoVistoso',
'atAvon',
'atFairfieldBay',
'atFairfieldGlade',
'atFairfieldMountains',
'atFairfieldSapphireValley',
'ReunionatOrlando',
'RioMar',
'RiversideSuites',
'RoyalGardenatWaikiki',
'RoyalSeaCliff',
'RoyalVista',
'SantaBarbara',
'SeaGardens',
'SeaWatchPlantation',
'Sedona',
'ShawneeVillageCrestview',
'ShawneeVillageDepuy',
'ShawneeVillageFairwayVillage',
'ShawneeVillageRidgeTop',
'ShawneeVillageRiverVillageI',
'ShawneeVillageRiverVillageII',
'Shearwater',
'SkylineTower',
'SmokyMountains',
'SmugglersNotchVermont',
'SouthShore',
'StThomas',
'StarIsland',
'StarrPassGolfSuites',
'SteamboatSprings',
'SundaraCottagesatWisconsinDells',
'SydneySuites',
'Tamarack',
'Taos',
'TheDonatello',
'TheLegacyGolf',
'TheMillsHouseGrandHotel',
'TheQueenMaryHotel',
'TowersontheGroveatNorthMyrtleBeach',
'TropicanaatLasVegas',
'VinoBello',
'VintageLandingatFourSeasons',
'Westwinds',
'WorldMarkAnaheim',
'WorldMarkAnaheimDolphinsCove',
'WorldMarkAngelsCamp',
'WorldMarkArrowPoint',
'WorldMarkBassLake',
'WorldMarkBearLake',
'WorldMarkBendSeventhMountain',
'WorldMarkBigBear',
'WorldMarkBirchBay',
'WorldMarkBisonRanch',
'WorldMarkBlaine',
'WorldMarkBranson',
'WorldMarkCanmoreBanff',
'WorldMarkCathedralCity',
'WorldMarkChelanLakeHouse',
'WorldMarkClearLake',
'WorldMarkCoralBaja',
'WorldMarkDaytonaBeachOceanWalk',
'WorldMarkDeerHarbor',
'WorldMarkDepoeBay',
'WorldMarkDiscoveryBay',
'WorldMarkEagleCrest',
'WorldMarkEstesPark',
'WorldMarkFiji',
'WorldMarkFortLauderdalePalmAire',
'WorldMarkFortLauderdaleSantaBarbara',
'WorldMarkFortLauderdaleSeaGardens',
'WorldMarkGalena',
'WorldMarkGleneden',
'WorldMarkGranbyRockyMountainPreserve',
'WorldMarkGrandLake',
'WorldMarkHavasuDunes',
'WorldMarkHuntStablewoodSprings',
'WorldMarkIndio',
'WorldMarkIslaMujeres',
'WorldMarkKapaaShore',
'WorldMarkKihei',
'WorldMarkKona',
'WorldMarkLaPaloma',
'WorldMarkLakeChelanShores',
'WorldMarkLakeTahoe',
'WorldMarkLakeoftheOzarks',
'WorldMarkLasVegasBoulevard',
'WorldMarkLasVegasSpencerStreet',
'WorldMarkLasVegasTropicanaAve',
'WorldMarkLeavenworth',
'WorldMarkLongBeach',
'WorldMarkMarbleFalls',
'WorldMarkMarinaDunes',
'WorldMarkMarinerVillage',
'WorldMarkMcCall',
'WorldMarkMidway',
'WorldMarkNewBraunfels',
'WorldMarkNewOrleansAvenuePlaza',
'WorldMarkOceanside',
'WorldMarkOrlandoKingstownReef',
'WorldMarkOrlandoReunion',
'WorldMarkPagosa',
'WorldMarkPalmSprings',
'WorldMarkPalmSpringsPlazapa',
'WorldMarkParkCity',
'WorldMarkPhoenixSouthMountainPreserve',
'WorldMarkPinetop',
'WorldMarkPismoBeach',
'WorldMarkRanchoVistoso',
'WorldMarkRedRiver',
'WorldMarkReno',
'WorldMarkRunningY',
'WorldMarkSanDiegoBalboaPark',
'WorldMarkSanDiegoInnatthePark',
'WorldMarkSanDiegoMissionValley',
'WorldMarkSanFrancisco',
'WorldMarkSantaFe',
'WorldMarkSchoonerLanding',
'WorldMarkScottsdale',
'WorldMarkSeaside',
'WorldMarkSeattleTheCamlin',
'WorldMarkSolvang',
'WorldMarkSouthPacificClubbyCairns',
'WorldMarkSouthShore',
'WorldMarkStGeorge',
'WorldMarkStThomasElysianBeach',
'WorldMarkSteamboatSprings',
'WorldMarkSurfsideInn',
'WorldMarkTaos',
'WorldMarkValleyIsle',
'WorldMarkVancouverTheCanadian',
'WorldMarkVictoria',
'WorldMarkWestYellowstone',
'WorldMarkWhistlerCascadeLodge',
'WorldMarkWindsor',
'WorldMarkWolfCreek',
'WorldMarkZihuatanejo',
'GlacierCanyon',
'MajesticSun',
'NationalHarbor',
'TheCottages',
'WaikikiBeachWalk');
my $portfolio = "VillaDelMar";
print STDOUT "The first portfolio is: $portfolio\n";
my @portfolioSizes = (10000,20000,30000,40000,50000,60000,70000,80000);
for(my $i = 1; $i <= $NumberOfRecords ; $i++ ){
    my $number = $account_number++;

    if(exists $unique{$number}){
        $unique{$number}++;    
    }else{	
     	if($portfolio_splitter != 0 && $portfolio_splitter % $RecordsPerPortfolio == 0){
        	 $portfolio =  $portfolioNames[rand @portfolioNames];
        	 print STDOUT "$portfolio_splitter new portfolio: $portfolio\n";
		$RecordsPerPortfolio = $portfolioSizes[rand @portfolioSizes];
	}
        my $amountDelinquent = 0;
        my $delinquent = $daysDelinquent[rand @daysDelinquent];
        if($delinquent gt 0){
            $amountDelinquent = $delinquentAmount[rand @delinquentAmount];
        }
        print CLIENT join("\t",$portfolio,$number,"Username".join('', map +(q(A)..q(Z))[rand(26)], 1..3), "100.00",$inventory[ rand @inventory ],$delinquent,$amountDelinquent,"\n");
        print OBLIGATIONS join("\t",$obligationCounter++,$number,"Maintenance","0"),"\n";
#        print OBLIGATIONS join("\t",$obligationCounter++,$number,"Loan","0"),"\n";
        $unique{$number}++;
    }    
     $portfolio_splitter++;

}
close(CLIENT);
close(OBLIGATIONS); 

for my $k (sort keys %unique) {
    print"Not so random: $k: $unique{$k}\n" if $unique{$k} != 1;
}
