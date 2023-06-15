const gulp = requireModule("gulp-with-help"),
  packageDir = require("./config").packageDir,
  path = require("path"),
  fs = require("fs"),
  runSequence = requireModule("run-sequence"),
  /** @type NugetPush */
  nugetPush = requireModule("nuget-push"),
  env = requireModule("env");

env.associate([ "DRY_RUN" ], [ "push" ]);

gulp.task("release", done => {
  runSequence("pack", "push", "commit-release", "tag-and-push", done);
});

gulp.task("push", "pushes packages to nuget.org", () => {
  const packages = [
      findNupkg("Quackers.TestLogger"),
    ].flat(),
    promises = packages.map(p => nugetPush(p));
  return Promise.all(promises);
});

function findNupkg(id) {
  return findPackage(id, "nupkg");
}

function findPackage(id, ext) {
  return fs
    .readdirSync(packageDir)
    .filter(p => p.endsWith(`.${ext}`))
    .filter(p => {
      const parts = p
        .split(".")
        .filter(part => part !== ext && isNaN(parseInt(part)));
      return parts.join(".") === id;
    })
    .map(p => path.join(packageDir, p))
    .sort()
    .reverse()[0];
}
