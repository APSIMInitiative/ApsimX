# Pull Request Process
Making a pull request will trigger a [build on Jenkins](http://apsrunet.apsim.info/JenkinsCI/) to test that your code changes have not introduced any problems.  If this automated build fails in any way your changes will not be merged into master.  To make sure that your pull request triggers a jenkins build it must include the words

	"working on #XXX"
or 

	"resolves #XXX"
    
where `XXX` is an issue number.  Thus, you must also have already logged a relevant issue to relate your pull request to or be fixing an existing issue.

To get Jenkins to retest a pull request you can type this into a comment:

	retest this please Jenkins
