<?php

// autoload_static.php @generated by Composer

namespace Composer\Autoload;

class ComposerStaticInitc6eec7121702415517f3fa9c99fbdd87
{
    public static $prefixLengthsPsr4 = array (
        'M' => 
        array (
            'Medoo\\' => 6,
        ),
    );

    public static $prefixDirsPsr4 = array (
        'Medoo\\' => 
        array (
            0 => __DIR__ . '/..' . '/catfan/medoo/src',
        ),
    );

    public static $classMap = array (
        'Composer\\InstalledVersions' => __DIR__ . '/..' . '/composer/InstalledVersions.php',
    );

    public static function getInitializer(ClassLoader $loader)
    {
        return \Closure::bind(function () use ($loader) {
            $loader->prefixLengthsPsr4 = ComposerStaticInitc6eec7121702415517f3fa9c99fbdd87::$prefixLengthsPsr4;
            $loader->prefixDirsPsr4 = ComposerStaticInitc6eec7121702415517f3fa9c99fbdd87::$prefixDirsPsr4;
            $loader->classMap = ComposerStaticInitc6eec7121702415517f3fa9c99fbdd87::$classMap;

        }, null, ClassLoader::class);
    }
}