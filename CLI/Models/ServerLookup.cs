using System.Collections.Generic;

namespace SqlAgent.Cli.Models;

public class ServerLookup() : Dictionary<string, string>(new Dictionary<string, string>()
{
	{ "d673", "RF3DDB673N1.ltdauto.intel.com,3180" },
	{ "s673", "RF3SDB673-UI-vl.rf3stg.mfgint.intel.com,3180" },
	{ "p673", "RF3PDB673-UI-vl.rf3prod.mfg.intel.com,3180" },
	{ "d672", "RF3DDB672N1.ltdauto.intel.com,3180" },
	{ "s672", "RF3SDB672-E3-vl.rf3stg.mfgint.intel.com,3180" },
	{ "p672", "RF3PDB672-E3-VL.rf3prod.mfg.intel.com,3180" },
});