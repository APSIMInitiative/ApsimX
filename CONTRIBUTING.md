# How to Contribute

This project is [licensed](LICENSE) and accepts contributions via
GitHub pull requests.  This document outlines some of the conventions on
development workflow, commit message formatting, contact points and other
resources to make it easier to get your contribution accepted.

## Communication

?

## Getting Started

- Fork the repository on GitHub
- Read the spec, submit bugs, submit improvements!
- Submit issues on Github to discuss proposed changes

## Contribution Flow

This is a rough outline of what a contributor's workflow looks like:

- Create a topic branch from where you want to base your work (usually master).
- Make commits of logical units.
- Make sure your commit messages are in the proper format (see below).
- Push your changes to a topic branch in your fork of the repository.
- Make sure the tests pass, and add any new tests as appropriate.
- Submit a pull request to the original repository.

Thanks for your contributions!

## Pull Requests

When you initiate a pull request Github will triger a build on 
[Jenkins](http://apsrunet.apsim.info/JenkinsCI/) to test that your code changes 
have not introdued any problems. If this fails in any way your pull request will 
not be merged into master.

### Format of the Commit Message

In order that your pull request triggers a Jenkins build it must include the words

	"Working on #XXX"
or 

	"resolved #XXX"
    
where XXX is an active GitHub issue.  Thus, you must also have logged a relevant GitHub
issue to relate your pull request to.
