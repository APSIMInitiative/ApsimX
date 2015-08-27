#Pull request process
When you initiate a pull request Github will triger a build on Jenkins (http://apsrunet.apsim.info/JenkinsCI/) to test that your code changes have not introdued any problems.  If this fails in any way your pull request will not be merged into master.  To make sure that your pull request triggers a jenkins build it must include the words

	"Working on #XXX"
or 

	"resloved #XXX"
    
where XXX is an active git hub issue.  Thus, you must also have loged a relavent git hub issue to relate your pull request to.